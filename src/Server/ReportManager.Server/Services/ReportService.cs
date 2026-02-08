using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Server.Services.Repository;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Configuration;
using System.Data;
using System.Globalization;

namespace ReportManager.Server.Services
{
	public sealed class ReportService
	{
        #region Private Fields

        private readonly ReportRepository _repo;

        #endregion

        #region Ctor

        public ReportService()
		{
			var cs = ConfigurationManager.ConnectionStrings["ReportDb"].ConnectionString;
			_repo = new ReportRepository(cs);
		}

        #endregion

        #region Public Methods

        public ReportManifestDto GetReportManifest(string reportKey)
		{
			var culture = CultureInfo.CurrentUICulture.Name;
            var def = _repo.GetReportDefinitionByKey(reportKey);
			var model = JsonUtil.Deserialize<ReportDefinitionJson>(def.DefinitionJson) ?? throw new InvalidOperationException("Report definition JSON is invalid.");
            var manifest = new ReportManifestDto
            {
                ReportKey = reportKey,
                Title = TextsResolver.ResolveText(model.Texts, KnownTextKeys.ReportTitle, culture, model.DefaultCulture)
            };

            // compute ops by type (server rules)
			foreach (var c in model.Columns)
			{
				var colType = c.Type;
				
				var displayName = TextsResolver.ResolveText(model.Texts, KnownTextKeys.GetColumnHeaderKey(c.Key), culture, model.DefaultCulture);
				var category = string.Empty;
				if (!string.IsNullOrWhiteSpace(c.Category))
				{
					var categoryTextKey = KnownTextKeys.GetColumnCategoryKey(c.Category);
					var resolvedCategory = TextsResolver.ResolveText(model.Texts, categoryTextKey, culture, model.DefaultCulture);
					category = string.Equals(resolvedCategory, categoryTextKey, StringComparison.OrdinalIgnoreCase)
						? c.Category
						: resolvedCategory;
				}

				var filterEnabled = c.Flags.HasFlag(ReportColumnFlagsJson.Filterable);
				var filterHidden = c.Filter?.Flags.HasFlag(FilterConfigFlagsJson.Hidden) == true;
				var hasLookup = filterEnabled && c.Filter?.Lookup != null;
				var sortHidden = c.Sort?.Flags.HasFlag(SortConfigFlagsJson.Hidden) == true;

				var col = new ReportColumnManifestDto
				{
					Key = c.Key,
					DisplayName = displayName,
					Category = category,
					Type = colType,
					Hidden = c.Flags.HasFlag(ReportColumnFlagsJson.Hidden),
					AlwaysSelect = c.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect),
					PrimaryKey = c.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey),
                    FilterEnabled = filterEnabled,
					FilterHidden = filterHidden,
					SortEnabled = c.Flags.HasFlag(ReportColumnFlagsJson.Sortable),
					SortHidden = sortHidden,
					FilterOps = filterEnabled ? ComputeOps(colType, hasLookup) : [],
					Lookup = null,
					Virtual = c.Flags.HasFlag(ReportColumnFlagsJson.Virtual)
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
									Text = TextsResolver.ResolveText(model.Texts, it.Text, culture, model.DefaultCulture)
								});
							}
						}
						col.Lookup = dto;
					}
					else if (lk.Mode == LookupMode.Sql && lk.Sql != null)
					{
						// validate it's a SELECT-only statement and forbid dangerous tokens.
						if (!SqlLookupValidator.TryValidate(lk.Sql.CommandText, out var error))
							throw new InvalidOperationException($"Lookup SQL for column '{c.Key}' is not allowed: {error}");

						var items = _repo.ExecuteLookup(lk.Sql.CommandText, lk.Sql.KeyColumn, lk.Sql.TextColumn);
						col.Lookup = new LookupDto { Items = items };
					}
				}

				manifest.Columns.Add(col);
			}

			return manifest;
		}

		public ReportPageDto QueryReport(ReportQueryRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request));
			if (request.PageSize == null) throw new ArgumentException("Page size must be specified", nameof(ReportQueryRequestDto.PageSize));

			return QueryReportInternal(request, ReportQueryFlags.None);
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

			// user presets only
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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Internal query method allowing null PageSize (used for downloads).
        /// </summary>
        internal ReportPageDto QueryReportInternal(ReportQueryRequestDto request, ReportQueryFlags queryFlags)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.PageSize != null)
            {
                if (request.PageSize <= 0) request.PageSize = 100;
                if (request.PageSize > 500) request.PageSize = 500;
                if (request.PageIndex < 0) request.PageIndex = 0;
            }

            var definition = _repo.GetReportDefinitionByKey(request.ReportKey);
            var model = JsonUtil.Deserialize<ReportDefinitionJson>(definition.DefinitionJson) ?? throw new InvalidOperationException("Report definition JSON is invalid.");

            // allowed columns
            var allowed = model.Columns
                .Where(c => !c.Flags.HasFlag(ReportColumnFlagsJson.Virtual))
                .Select(c => new SqlQueryBuilder.ColumnInfo(
                c.Key,
                c.Type,
                c.Flags.HasFlag(ReportColumnFlagsJson.Filterable),
                c.Flags.HasFlag(ReportColumnFlagsJson.Sortable),
                c.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey))
            ).ToList();

            // ensure query
            var query = request.Query ?? new();

            // select list: requested or all non-hidden + alwaysSelect
            var selected = EnsureSelected(model, query, queryFlags);

            // ensure sorting
            EnsureSorting(model, allowed, query);

            var (countSql, countParams) = SqlQueryBuilder.BuildCount(definition.ViewSchema, definition.ViewName, allowed, query);
            int total = _repo.ExecuteScalarInt(countSql, countParams);

            var (sql, prms) = SqlQueryBuilder.BuildPagedSelect(definition.ViewSchema, definition.ViewName, selected, allowed, query, request.PageIndex, request.PageSize);
            var dt = _repo.ExecuteDataTable(sql, prms);
            dt.TableName = "Rows";
            FillColumnNames(model, dt, CultureInfo.CurrentUICulture.Name);

            return new ReportPageDto { Rows = dt, TotalCount = total };
        }

        #endregion

        #region Private Methods

        private List<string> EnsureSelected(ReportDefinitionJson model, QuerySpecDto query, ReportQueryFlags queryFlags)
        {
            var selected = new List<string>();

            // primaryKeyOnly mode
			if (queryFlags.HasFlag(ReportQueryFlags.SelectPrimaryKeyOnly))
			{
                // Getting only the primary key column - skip all the other ones (like alwaysAdd columns)
                var pkColumn = model.Columns.FirstOrDefault(c => c.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey));
				if (pkColumn == null)
				{
					throw new InvalidOperationException("Report does not have a primary key column.");
                }

				selected.Add(pkColumn.Key);
				return selected;
            }

            if (query.SelectedColumns != null && query.SelectedColumns.Count > 0)
                selected.AddRange(query.SelectedColumns);

            if (selected.Count == 0)
            {
                foreach (var c in model.Columns)
                    if (!c.Flags.HasFlag(ReportColumnFlagsJson.Hidden) || c.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect))
                        selected.Add(c.Key);
            }

            // ensure alwaysSelect
            foreach (var c in model.Columns.Where(x => x.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect)))
                if (!selected.Contains(c.Key, StringComparer.OrdinalIgnoreCase))
                    selected.Add(c.Key);

            return selected;
        }

        private void EnsureSorting(ReportDefinitionJson model, List<SqlQueryBuilder.ColumnInfo> allowed, QuerySpecDto query)
        {
            query.Sorting ??= [];
            if (query.Sorting.Count == 0)
            {
                var defaultSort = model.DefaultSort;
                if (defaultSort != null && defaultSort.Count > 0)
                {
                    query.Sorting = defaultSort.Select(x => (SortSpecDto)x).ToList();
                }
                else
                {
                    // stable order required by OFFSET/FETCH; use PK if available
                    var pkCol = allowed.FirstOrDefault(c => c.PrimaryKey /*&& c.SortEnabled*/); // sort doesnt have to be enabled for PK
                    if (pkCol != null)
                        query.Sorting.Add(new SortSpecDto() { ColumnKey = pkCol.Key, Direction = SortDirection.Asc });
                }
            }
        }

        private void FillColumnNames(ReportDefinitionJson definition, DataTable table, string culture)
        {
            foreach (DataColumn col in table.Columns)
            {
                var colDef = definition.Columns.FirstOrDefault(c => string.Equals(c.Key, col.ColumnName, StringComparison.OrdinalIgnoreCase));
                if (colDef != null)
                {
                    col.Caption = TextsResolver.ResolveText(definition.Texts, KnownTextKeys.GetColumnHeaderKey(colDef.Key), culture, definition.DefaultCulture);
                }
            }
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
				case ReportColumnType.Integer:
				case ReportColumnType.Long:
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

				case ReportColumnType.Boolean:
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

			// 1) Grid.HiddenColumns: allow only columns that are default-visible and not alwaysSelect
			var allowedHide = model.Columns
				.Where(c => !c.Flags.HasFlag(ReportColumnFlagsJson.Hidden) && !c.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect))
				.Select(c => c.Key)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			content.Grid.HiddenColumns = (content.Grid.HiddenColumns ?? new List<string>())
				.Where(k => allowedHide.Contains(k))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();

			// 2) Query.Filters: drop invalid entries
			var normalizedFilters = new List<FilterSpecDto>();
			foreach (var f in content.Query.Filters ?? [])
			{
				if (f == null || string.IsNullOrWhiteSpace(f.ColumnKey)) continue;
				if (!cols.TryGetValue(f.ColumnKey, out var c)) continue;

				bool filterEnabled = c.Flags.HasFlag(ReportColumnFlagsJson.Filterable);
				if (!filterEnabled) continue;

				// allowed ops by type (server rules)
				var type = c.Type;
				var hasLookup = c.Filter!.Lookup != null;
				var allowedOps = ComputeOps(type, hasLookup);
				if (!allowedOps.Contains(f.Operation)) continue;

				// normalize values count
				var values = f.Values ?? [];
				values = values.Select(v => (v ?? string.Empty).Trim()).Where(v => v.Length > 0).ToList();

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
					// soft limit (adjust as needed)
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

			// 3) Query.Sorting: Remove invalid
			var normalizedSorting = new List<SortSpecDto>();
			foreach (var s in content.Query.Sorting ?? new List<SortSpecDto>())
			{
				if (s == null || string.IsNullOrWhiteSpace(s.ColumnKey)) continue;
				if (!cols.TryGetValue(s.ColumnKey, out var c)) continue;

				bool sortEnabled = c.Flags.HasFlag(ReportColumnFlagsJson.Sortable);
				if (!sortEnabled) continue;

				normalizedSorting.Add(new SortSpecDto
				{
					ColumnKey = c.Key,
					Direction = s.Direction
				});
			}
			content.Query.Sorting = normalizedSorting;

			// 4) SelectedColumns: Not used in presets.
			// Let the server decide based on hidden + alwaysSelect.
			content.Query.SelectedColumns = new List<string>();
        }

        #endregion
    }
}
