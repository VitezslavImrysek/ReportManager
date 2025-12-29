using ReportManager.ApiContracts.Dto;
using ReportManager.ApiContracts.Services;
using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Server.Repository;
using System.Configuration;

namespace ReportManager.Server
{
	public sealed class ReportService : IReportService
	{
		// For demo: hardcoded connection string name injected by host
		private readonly ReportRepository _repo;

		public ReportService()
		{
			var cs = ConfigurationManager.ConnectionStrings["ReportDb"].ConnectionString;
			_repo = new ReportRepository(cs);
		}

		public ReportManifestDto GetReportManifest(string reportKey, string culture)
		{
			var def = _repo.GetReportDefinitionByKey(reportKey);
			var model = JsonUtil.Deserialize<ReportDefinitionJson>(def.DefinitionJson) ?? throw new InvalidOperationException("Report definition JSON is invalid.");
			var manifest = new ReportManifestDto
			{
				ReportKey = reportKey,
				Culture = NormalizeCulture(culture, model.DefaultCulture),
				Version = def.Version
			};
			manifest.Title = ResolveText(model, manifest.Culture, model.TextKey);

			// compute ops by type (server rules)
			foreach (var c in model.Columns)
			{
				var colType = c.Type;
				
				var displayName = ResolveText(model, manifest.Culture, c.TextKey);

				var filterEnabled = c.Filter != null && c.Filter.Enabled;
				var hasLookup = filterEnabled && c.Filter!.Lookup != null;

				var col = new ReportColumnManifestDto
				{
					Key = c.Key,
					DisplayName = displayName,
					Type = colType,
					Hidden = c.Hidden,
					AlwaysSelect = c.AlwaysSelect,
					FilterEnabled = filterEnabled,
					SortEnabled = c.Sort != null && c.Sort.Enabled,
					FilterOps = filterEnabled ? ComputeOps(colType, hasLookup) : [],
					Lookup = null
				};

				// resolve lookup items (static or sql) INTO manifest
				if (hasLookup)
				{
					var lk = c.Filter!.Lookup!;
					if (lk.Mode == LookupMode.Static)
					{
						var dto = new LookupDto();
						if (lk.Items != null)
						{
							foreach (var it in lk.Items)
							{
								dto.Items.Add(new LookupItemDto
								{
									Key = Convert.ToString(it.Key),
									Text = ResolveText(model, manifest.Culture, it.Text)
								});
							}
						}
						col.Lookup = dto;
					}
					else if (lk.Mode == LookupMode.Sql && lk.Sql != null)
					{
						// For demo: trust admin-provided SELECT.
						// In production: validate it's a SELECT-only statement and forbid dangerous tokens.
						if (!SqlLookupValidator.TryValidate(lk.Sql.CommandText, out var error))
							throw new InvalidOperationException($"Lookup SQL for column '{c.Key}' is not allowed: {error}");

						var items = _repo.ExecuteLookup(lk.Sql.CommandText, lk.Sql.KeyColumn, lk.Sql.TextColumn);
						col.Lookup = new LookupDto { Items = items };
					}
				}

				manifest.Columns.Add(col);
			}

			if (model.DefaultSort != null)
			{
				foreach (var s in model.DefaultSort)
				{
					manifest.DefaultSort.Add(new SortSpecDto
					{
						ColumnKey = s.Column,
						Direction = s.Dir
					});
				}
			}

			return manifest;
		}

		public ReportPageDto QueryReport(ReportQueryRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));
			if (request.PageSize == null) throw new ArgumentException("Page size must be specified", nameof(ReportQueryRequestDto.PageSize));

			return QueryReportInternal(request);
        }

        /// <summary>
        /// Internal query method allowing null PageSize (used for downloads).
        /// </summary>
        internal ReportPageDto QueryReportInternal(ReportQueryRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
			if (request.PageSize != null)
			{
                if (request.PageSize <= 0) request.PageSize = 100;
                if (request.PageSize > 500) request.PageSize = 500;
                if (request.PageIndex < 0) request.PageIndex = 0;
            }

            var def = _repo.GetReportDefinitionByKey(request.ReportKey);
            var model = JsonUtil.Deserialize<ReportDefinitionJson>(def.DefinitionJson) ?? throw new InvalidOperationException("Report definition JSON is invalid.");

            // allowed columns
            var allowed = model.Columns.Select(c => new SqlQueryBuilder.ColumnInfo
            {
                Key = c.Key,
                Type = c.Type,
                FilterEnabled = c.Filter != null && c.Filter.Enabled,
                SortEnabled = c.Sort != null && c.Sort.Enabled
            }).ToList();

            // select list: requested or all non-hidden + alwaysSelect
            var selected = new List<string>();
            if (request.Query != null && request.Query.SelectedColumns != null && request.Query.SelectedColumns.Count > 0)
                selected.AddRange(request.Query.SelectedColumns);

            if (selected.Count == 0)
            {
                foreach (var c in model.Columns)
                    if (!c.Hidden || c.AlwaysSelect)
                        selected.Add(c.Key);
            }

            // ensure alwaysSelect
            foreach (var c in model.Columns.Where(x => x.AlwaysSelect))
                if (!selected.Contains(c.Key, StringComparer.OrdinalIgnoreCase))
                    selected.Add(c.Key);

            var (countSql, countParams) = SqlQueryBuilder.BuildCount(def.ViewSchema, def.ViewName, allowed, request.Query ?? new QuerySpecDto());
            int total = _repo.ExecuteScalarInt(countSql, countParams);

            var (sql, prms) = SqlQueryBuilder.BuildPagedSelect(def.ViewSchema, def.ViewName, selected, allowed, request.Query ?? new QuerySpecDto(), request.PageIndex, request.PageSize);
            var dt = _repo.ExecuteDataTable(sql, prms);
            dt.TableName = "Rows";

            return new ReportPageDto { Rows = dt, TotalCount = total };
        }

        public List<PresetInfoDto> GetPresets(string reportKey, Guid userId)
			=> _repo.GetPresets(reportKey, userId);

		public PresetDto GetPreset(Guid presetId, Guid userId)
			=> _repo.GetPreset(presetId, userId);

		public Guid SavePreset(SavePresetRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));
			if (request.Preset == null) throw new ArgumentNullException(nameof(request.Preset));
			if (string.IsNullOrWhiteSpace(request.Preset.ReportKey)) throw new ArgumentException("ReportKey is required");

			// user presets only in demo
			request.Preset.IsSystem = false;

			// Validate+normalize content against report definition
			var def = _repo.GetReportDefinitionByKey(request.Preset.ReportKey);
			var model = JsonUtil.Deserialize<ReportDefinitionJson>(def.DefinitionJson) ?? throw new InvalidOperationException("Report definition JSON is invalid.");
			NormalizePresetContent(request.Preset.Content, model);

			return _repo.SavePreset(request.Preset, request.UserId);
		}

		public void DeletePreset(Guid presetId, Guid userId)
			=> _repo.DeletePreset(presetId, userId);

		public void SetDefaultPreset(Guid presetId, string reportKey, Guid userId)
			=> _repo.SetDefaultPreset(presetId, reportKey, userId);

		private static string NormalizeCulture(string culture, string defaultCulture)
		{
			if (!string.IsNullOrWhiteSpace(culture))
			{
				// keep language part only for simplicity (cs-CZ -> cs)
				var dash = culture.IndexOf('-');
				if (dash > 0) culture = culture.Substring(0, dash);
				return culture.ToLowerInvariant();
			}
			return string.IsNullOrWhiteSpace(defaultCulture) ? "en" : defaultCulture.ToLowerInvariant();
		}

		private static string ResolveText(ReportDefinitionJson model, string culture, string? textKey)
		{
			if (string.IsNullOrEmpty(textKey))
			{
				return string.Empty;
			}

			if (model.Texts != null)
			{
				Dictionary<string, string> dict;
				if (model.Texts.TryGetValue(culture, out dict) && dict != null && dict.TryGetValue(textKey!, out var t) && !string.IsNullOrEmpty(t))
					return t;

				if (!string.IsNullOrWhiteSpace(model.DefaultCulture)
					&& model.Texts.TryGetValue(model.DefaultCulture, out dict)
					&& dict != null
					&& dict.TryGetValue(textKey!, out t)
					&& !string.IsNullOrEmpty(t))
					return t;

				// fallback to any
				foreach (var kv in model.Texts)
				{
					dict = kv.Value;
					if (dict != null && dict.TryGetValue(textKey!, out t) && !string.IsNullOrEmpty(t))
						return t;
				}
			}

			return textKey!;
		}

		private static List<FilterOperation> ComputeOps(ReportColumnType type, bool hasLookup)
		{
			if (hasLookup)
			{
				return new List<FilterOperation>
					{
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.In, FilterOperation.NotIn,
						FilterOperation.IsNull, FilterOperation.NotNull
					};
			}

			// Simple default rules
			switch (type)
			{
				case ReportColumnType.Int32:
				case ReportColumnType.Int64:
				case ReportColumnType.Decimal:
				case ReportColumnType.Double:
					return new List<FilterOperation>
					{
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.Gt, FilterOperation.Ge, FilterOperation.Lt, FilterOperation.Le,
						FilterOperation.Between,
						FilterOperation.In, FilterOperation.NotIn,
						FilterOperation.IsNull, FilterOperation.NotNull
					};

				case ReportColumnType.Date:
				case ReportColumnType.DateTime:
					return new List<FilterOperation>
					{
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.Gt, FilterOperation.Ge, FilterOperation.Lt, FilterOperation.Le,
						FilterOperation.Between,
						FilterOperation.IsNull, FilterOperation.NotNull
					};

				case ReportColumnType.Bool:
					return new List<FilterOperation>
					{
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.IsNull, FilterOperation.NotNull
					};

				case ReportColumnType.Guid:
					return new List<FilterOperation>
					{
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.In, FilterOperation.NotIn,
						FilterOperation.IsNull, FilterOperation.NotNull
					};

				case ReportColumnType.String:
				default:
					return new List<FilterOperation>
					{
						FilterOperation.Contains, FilterOperation.NotContains,
						FilterOperation.StartsWith, FilterOperation.EndsWith,
						FilterOperation.Eq, FilterOperation.Ne,
						FilterOperation.In, FilterOperation.NotIn,
						FilterOperation.IsNull, FilterOperation.NotNull
					};
			}
		}

		private void NormalizePresetContent(PresetContentDto content, ReportDefinitionJson model)
		{
			if (content == null) content = new PresetContentDto();
			if (content.Query == null) content.Query = new QuerySpecDto();
			if (content.Grid == null) content.Grid = new GridStateDto();

			// Map columns by key
			var cols = model.Columns.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

			// 1) Grid.HiddenColumns: dovol jen sloupce, které jsou default-visible a nejsou alwaysSelect
			var allowedHide = model.Columns
				.Where(c => !c.Hidden && !c.AlwaysSelect)
				.Select(c => c.Key)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			content.Grid.HiddenColumns = (content.Grid.HiddenColumns ?? new List<string>())
				.Where(k => allowedHide.Contains(k))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			// 2) Query.Filters: vyházej nevalidní
			var normalizedFilters = new List<FilterSpecDto>();
			foreach (var f in content.Query.Filters ?? new List<FilterSpecDto>())
			{
				if (f == null || string.IsNullOrWhiteSpace(f.ColumnKey)) continue;
				if (!cols.TryGetValue(f.ColumnKey, out var c)) continue;

				bool filterEnabled = c.Filter != null && c.Filter.Enabled;
				if (!filterEnabled) continue;

				// allowed ops by type (server rules)
				var type = c.Type;
				var hasLookup = c.Filter!.Lookup != null;
				var allowedOps = ComputeOps(type, hasLookup);
				if (!allowedOps.Contains(f.Operation)) continue;

				// normalize values count
				var values = f.Values ?? [];
				values = values.Select(v => (v ?? "").Trim()).Where(v => v.Length > 0).ToList();

				if (f.Operation == FilterOperation.IsNull || f.Operation == FilterOperation.NotNull)
				{
					values.Clear();
				}
				else if (f.Operation == FilterOperation.Between)
				{
					if (values.Count < 2) continue;
					values = values.Take(2).ToList();
				}
				else if (f.Operation == FilterOperation.In || f.Operation == FilterOperation.NotIn)
				{
					// soft limit (upravit dle potřeby)
					if (values.Count == 0) continue;
					values = values.Distinct(StringComparer.OrdinalIgnoreCase).Take(500).ToList();
				}
				else
				{
					if (values.Count < 1) continue;
					values = values.Take(1).ToList();
				}

				normalizedFilters.Add(new FilterSpecDto
				{
					ColumnKey = c.Key,               // normalized key
					Operation = f.Operation,
					Values = values
				});
			}
			content.Query.Filters = normalizedFilters;

			// 3) Query.Sorting: vyházej nevalidní
			var normalizedSorting = new List<SortSpecDto>();
			foreach (var s in content.Query.Sorting ?? new List<SortSpecDto>())
			{
				if (s == null || string.IsNullOrWhiteSpace(s.ColumnKey)) continue;
				if (!cols.TryGetValue(s.ColumnKey, out var c)) continue;

				bool sortEnabled = c.Sort != null && c.Sort.Enabled;
				if (!sortEnabled) continue;

				normalizedSorting.Add(new SortSpecDto
				{
					ColumnKey = c.Key,
					Direction = s.Direction
				});
			}
			content.Query.Sorting = normalizedSorting;

			// 4) SelectedColumns: nepouštěl bych z presetu vůbec (zjednodušení)
			// Nech server dál rozhodovat podle hidden + alwaysSelect (přes tvůj UI stav).
			content.Query.SelectedColumns = new List<string>();

			// Ensure version
			if (content.Version <= 0) content.Version = 1;
		}
	}
}
