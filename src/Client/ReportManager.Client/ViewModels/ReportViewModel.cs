using ReportManager.Client.Extensions;
using ReportManager.Proxy.Services;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.ServiceModel;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
	public sealed class ReportViewModel : NotificationObject
	{
        #region Private Fields

        private static readonly bool IsInDesignMode =
            DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());

        private readonly ChannelFactory<IReportService> _factory;
		private readonly ChannelFactory<IReportDownloadService> _reportDownloadFactory;
		private readonly IReportService _svc;
		private readonly IReportDownloadService _downloadSvc;

		private int _pageIndex = 0;
		private int _totalCount = 0;

        #endregion

        #region Ctor

        public ReportViewModel()
		{
            // Skip when in WPF design mode
			if (!IsInDesignMode)
			{
                _factory = ServicesConfiguration.CreateChannelFactory<IReportService>();
                _reportDownloadFactory = ServicesConfiguration.CreateChannelFactory<IReportDownloadService>();
                _svc = _factory.CreateChannel();
                _downloadSvc = _reportDownloadFactory.CreateChannel();
            }

			LoadManifestCommand = new RelayCommand(LoadManifest);
			QueryCommand = new RelayCommand(Query);
			ClearServerQueryCommand = new RelayCommand(ClearServerQuery);
			AddConditionCommand = new RelayCommand(AddCondition);
			AddSortCommand = new RelayCommand(AddSort);
			PrevPageCommand = new RelayCommand(() => { if (_pageIndex > 0) { _pageIndex--; Query(); } });
			NextPageCommand = new RelayCommand(() => { if ((_pageIndex + 1) * PageSize < _totalCount) { _pageIndex++; Query(); } });
			LoadPresetCommand = new RelayCommand(LoadPreset);
			SavePresetCommand = new RelayCommand(SavePreset);
			OverwritePresetCommand = new RelayCommand(OverwritePreset);
			DownloadReportCsvCommand = new RelayCommand(() => DownloadReport(FileFormat.Csv));
			DownloadReportXlsxCommand = new RelayCommand(() => DownloadReport(FileFormat.Xlsx));
			DownloadReportPdfCommand = new RelayCommand(() => DownloadReport(FileFormat.Pdf));
			DownloadReportJsonCommand = new RelayCommand(() => DownloadReport(FileFormat.Json));
            DownloadPrimaryKeysCommand = new RelayCommand(() => DownloadPrimaryKeys());

            // initial load
            LoadManifest();
			Query();
		}

        #endregion

        #region Properties

        private Guid UserId => Guid.TryParse(UserIdText, out var g) ? g : Guid.Empty;
        private int PageSize => int.TryParse(PageSizeText, out var x) ? Math.Max(1, Math.Min(500, x)) : 100;

        public string ReportKey { get; set => SetValue(ref field, value); } = "Contracts";
        public string UserIdText { get; set => SetValue(ref field, value); } = Guid.Parse("11111111-1111-1111-1111-111111111111").ToString();
        public string PageSizeText { get; set => SetValue(ref field, value); } = "100";
        public string StatusText { get; set => SetValue(ref field, value); } = "Ready";

        public DataView? RowsView { get; set => SetValue(ref field, value); }

        public ReportManifestDto? Manifest { get; private set => SetValue(ref field, value); }

        public ObservableCollection<ColumnOption> AvailableColumns { get; set; } = [];
        public ObservableCollection<ColumnVisibilityItem> ColumnVisibility { get; } = [];
        public List<string> ColumnOrder { get; set => SetValue(ref field, value); } = [];

        public ObservableCollection<QueryConditionViewModel> Conditions { get; } = [];
        public ObservableCollection<SortSpecViewModel> Sorts { get; } = [];
        public ObservableCollection<FilterSpecDto> HiddenFilters { get; } = [];
        public ObservableCollection<SortSpecDto> HiddenSorts { get; } = [];

        public ObservableCollection<PresetInfoDto> Presets { get; } = [];
        public PresetInfoDto? SelectedPreset { get; set => SetValue(ref field, value); }
        public string NewPresetName { get; set; } = "Můj pohled";

        #endregion

        #region Commands

        public ICommand LoadManifestCommand { get; }
        public ICommand QueryCommand { get; }
        public ICommand ClearServerQueryCommand { get; }
        public ICommand AddConditionCommand { get; }
        public ICommand AddSortCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand SavePresetCommand { get; }
		public ICommand OverwritePresetCommand { get; }
        public ICommand DownloadReportCsvCommand { get; }
        public ICommand DownloadReportXlsxCommand { get; }
        public ICommand DownloadReportPdfCommand { get; }
        public ICommand DownloadReportJsonCommand { get; }
        public ICommand DownloadPrimaryKeysCommand { get; }

        #endregion

        #region Private Methods

        private void DownloadReport(FileFormat format)
		{
			if (Manifest == null) return;

			var query = BuildQuerySpec(Manifest);
			if (query == null) return;
			var req = new ReportDownloadRequestDto
			{
				ReportQuery = new ReportQueryRequestDto()
				{
                    ReportKey = ReportKey,
                    Query = query,
					PageSize = null,
                },
				FileFormat = format
			};
			using var stream = _downloadSvc.DownloadReport(req);

			// save to file
			var fileName = GetReportFileName(format);
			var filter = GetReportDialogFilter(format);
			var dlg = new Microsoft.Win32.SaveFileDialog
			{
				FileName = fileName,
				Filter = filter
			};
			if (dlg.ShowDialog() == true)
			{
				using (var fileStream = System.IO.File.Create(dlg.FileName))
				{
					stream.CopyTo(fileStream);
				}
				StatusText = "Report exported to: " + dlg.FileName;
			}
		}

        private void DownloadPrimaryKeys()
        {
            if (Manifest == null) return;

            var query = BuildQuerySpec(Manifest);
            if (query == null) return;
            var req = new ReportQueryRequestDto()
            {
                ReportKey = ReportKey,
                Query = query,
                PageSize = null,
            };
            using var stream = _downloadSvc.DownloadPrimaryKeyList(req);

            // save to file
            var filter = GetReportDialogFilter(FileFormat.Json);
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"{ReportKey}-primary-keys.json",
                Filter = filter
            };
            if (dlg.ShowDialog() == true)
            {
                using (var fileStream = System.IO.File.Create(dlg.FileName))
                {
                    stream.CopyTo(fileStream);
                }
                StatusText = "Primary keys exported to: " + dlg.FileName;
            }
        }

        private string GetReportDialogFilter(FileFormat format)
		{
			return format switch
			{
				FileFormat.Csv => "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
				FileFormat.Xlsx => "Excel files (*.xlsx)|*.xlsx|All files (*.*)|*.*",
				FileFormat.Pdf => "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
				FileFormat.Json => "JSON files (*.json)|*.json|All files (*.*)|*.*",
				_ => throw new NotImplementedException(),
			};
		}

		private string GetReportFileName(FileFormat format)
		{
			return format switch
			{
				FileFormat.Csv => $"{ReportKey}.csv",
				FileFormat.Xlsx => $"{ReportKey}.xlsx",
				FileFormat.Pdf => $"{ReportKey}.pdf",
				FileFormat.Json => $"{ReportKey}.json",
				_ => throw new NotImplementedException(),
			};
		}

		private void LoadManifest()
		{
			try
			{
				Manifest = _svc.GetReportManifest(ReportKey);
				AvailableColumns = Manifest.Columns.Select(c => new ColumnOption
				{
					Key = c.Key,
					DisplayName = c.DisplayName,
					Type = c.Type,
					CanFilter = c.FilterEnabled,
					FilterHidden = c.FilterHidden,
					CanSort = c.SortEnabled,
					SortHidden = c.SortHidden,
					IsHidden = c.Hidden,
					Ops = c.FilterOps?.ToObservable() ?? [],
					HasLookup = c.Lookup != null,
					LookupItems = c.Lookup?.Items?.ToObservable() ?? []
				}).ToObservable();

				// Load presets list
				Presets.Clear();
				foreach (var p in _svc.GetPresets(ReportKey, UserId))
					Presets.Add(p);

				// reset conditions/sorts
				Conditions.Clear();
				Sorts.Clear();
				HiddenFilters.Clear();
				HiddenSorts.Clear();

				// setup column visibility options
				ColumnVisibility.Clear();
				foreach (var c in Manifest.Columns)
				{
					// uživatel smí ovládat jen sloupce, které jsou "běžně viditelné" a nejsou alwaysSelect
					if (c.Hidden) continue;
					if (c.AlwaysSelect) continue;

					ColumnVisibility.Add(new ColumnVisibilityItem
					{
						Key = c.Key,
						DisplayName = c.DisplayName,
						IsVisible = true // default: viditelné
					});
				}

				StatusText = $"Manifest načten: {Manifest.ReportKey})";
			}
			catch (Exception ex)
			{
				StatusText = "LoadManifest error: " + ex.Message;
			}
		}

		private void Query()
		{
			if (Manifest == null) return;

			try
			{
				var query = BuildQuerySpec(Manifest);
				if (query == null) return;

				var req = new ReportQueryRequestDto
				{
					ReportKey = ReportKey,
					Query = query,
					PageIndex = _pageIndex,
					PageSize = PageSize
				};

				var res = _svc.QueryReport(req);
				_totalCount = res.TotalCount;

				RowsView = res.Rows.DefaultView;
				StatusText = $"Rows: {res.Rows.Rows.Count} / Total: {_totalCount} / Page: {_pageIndex + 1}";
			}
			catch (Exception ex)
			{
				StatusText = "Query error: " + ex.Message;
			}
		}

		private QuerySpecDto? BuildQuerySpec(ReportManifestDto manifest)
		{
			var q = new QuerySpecDto();

			// Selected columns = viditelné sloupce z gridu + vždy AlwaysSelect
			q.SelectedColumns.Clear();

			// 1) viditelné (user toggle)
			foreach (var c in manifest.Columns)
			{
				if (c.Hidden) continue;

				// pokud je to user-toggle sloupec, respektuj checkbox
				if (!c.AlwaysSelect)
				{
					var vis = ColumnVisibility.FirstOrDefault(x => x.Key.Equals(c.Key, StringComparison.OrdinalIgnoreCase));
					if (vis != null && !vis.IsVisible)
						continue;
				}

				// je viditelný => tahat
				q.SelectedColumns.Add(c.Key);
			}

			// 2) alwaysSelect vždy přidat (i když hidden)
			foreach (var c in manifest.Columns.Where(x => x.AlwaysSelect))
			{
				if (!q.SelectedColumns.Contains(c.Key, StringComparer.OrdinalIgnoreCase))
					q.SelectedColumns.Add(c.Key);
			}

			if (!AddVisibleFilters(q))
			{
				return null;
			}

			AddHiddenFilters(q);
			AddVisibleSorts(q);
			AddHiddenSorts(q);

			// Selected columns: use currently visible columns from manifest defaults (empty => server decides)
			return q;
		}

		private void ClearServerQuery()
		{
			Conditions.Clear();
			Sorts.Clear();
			_pageIndex = 0;

			Query();
		}

		private void AddCondition()
		{
			if (AvailableColumns.Count == 0) return;
			var vm = new QueryConditionViewModel
			{
				AvailableColumns = GetFilterableColumns(),
				SelectedColumn = GetFilterableColumns().FirstOrDefault(),
			};
			vm.RemoveCommand = new RelayCommand(() => Conditions.Remove(vm));
			Conditions.Add(vm);
		}

		private void AddSort()
		{
			if (AvailableColumns.Count == 0) return;
			var vm = new SortSpecViewModel
			{
				AvailableColumns = GetSortableColumns(),
				SelectedColumn = GetSortableColumns().FirstOrDefault(),
				SelectedDirection = SortDirection.Asc
			};
			vm.RemoveCommand = new RelayCommand(() => Sorts.Remove(vm));
			Sorts.Add(vm);
		}

		private ObservableCollection<ColumnOption> GetSortableColumns()
		{
			return AvailableColumns.Where(x => x.CanSort && !x.SortHidden).ToObservable();
		}

		private ObservableCollection<ColumnOption> GetFilterableColumns()
		{
			return AvailableColumns.Where(x => x.CanFilter && !x.FilterHidden).ToObservable();
		}

		private void LoadPreset()
		{
			try
			{
				if (SelectedPreset == null) return;

				var preset = _svc.GetPreset(SelectedPreset.PresetId, UserId);
				var content = preset.Content ?? new PresetContentDto();

				// 1) apply query
				Conditions.Clear();
				Sorts.Clear();
				HiddenFilters.Clear();
				HiddenSorts.Clear();

				foreach (var f in content.Query.Filters ?? new List<FilterSpecDto>())
				{
					var col = AvailableColumns.FirstOrDefault(x => x.Key.Equals(f.ColumnKey, StringComparison.OrdinalIgnoreCase));
					if (col == null) continue;

					if (col.FilterHidden && col.CanFilter)
					{
						HiddenFilters.Add(f);
						continue;
					}

					var vm = new QueryConditionViewModel
					{
						AvailableColumns = GetFilterableColumns(),
                        SelectedColumn = col,
						SelectedOp = f.Operation
					};
					vm.RemoveCommand = new RelayCommand(() => Conditions.Remove(vm));

					if (f.Operation == FilterOperation.Between && f.Values != null && f.Values.Count >= 2)
					{
						vm.Value1 = f.Values[0];
						vm.Value2 = f.Values[1];
					}
					else if ((f.Operation == FilterOperation.In || f.Operation == FilterOperation.NotIn) && f.Values != null)
					{
						vm.Value1 = string.Join(",", f.Values);
					}
					else if (f.Values != null && f.Values.Count >= 1)
					{
						vm.Value1 = f.Values[0];
					}

					if (col.HasLookup
						&& (vm.SelectedOp == FilterOperation.Eq || vm.SelectedOp == FilterOperation.Ne)
						&& !string.IsNullOrWhiteSpace(vm.Value1))
					{
						vm.SelectedLookupItem = col.LookupItems.FirstOrDefault(item =>
							string.Equals(item.Key, vm.Value1, StringComparison.OrdinalIgnoreCase));
					}

					Conditions.Add(vm);
				}

				foreach (var s in content.Query.Sorting ?? [])
				{
					var col = AvailableColumns.FirstOrDefault(x => x.Key.Equals(s.ColumnKey, StringComparison.OrdinalIgnoreCase));
					if (col == null) continue;

					if (col.SortHidden && col.CanSort)
					{
						HiddenSorts.Add(s);
						continue;
					}

					var vm = new SortSpecViewModel
					{
						AvailableColumns = GetSortableColumns(),
						SelectedColumn = col,
						SelectedDirection = s.Direction
					};
					vm.RemoveCommand = new RelayCommand(() => Sorts.Remove(vm));
					Sorts.Add(vm);
				}

                // 2) apply grid hidden columns + order
                var hidden = new HashSet<string>(content.Grid?.HiddenColumns ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                foreach (var cv in ColumnVisibility)
                    cv.IsVisible = !hidden.Contains(cv.Key);

                if (content.Grid?.Order is { Count: > 0 })
                {
					var orderLookup = content.Grid.Order
						.Select((key, index) => new { key, index })
						.ToDictionary(x => x.key, x => x.index, StringComparer.OrdinalIgnoreCase);
					var ordered = ColumnVisibility
						.Select((item, index) => new { item, index })
						.OrderBy(x => orderLookup.TryGetValue(x.item.Key, out var orderIndex) ? orderIndex : int.MaxValue)
						.ThenBy(x => x.index)
						.Select(x => x.item)
						.ToList();

					ColumnVisibility.Clear();
					foreach (var item in ordered)
						ColumnVisibility.Add(item);

					ColumnOrder = new List<string>(content.Grid.Order);
                }

                _pageIndex = 0;
				Query();
			}
			catch (Exception ex)
			{
				StatusText = "LoadPreset error: " + ex.Message;
			}
		}

		private void SavePreset()
		{
			if (Manifest == null) return;

			try
			{
				var query = BuildQuerySpec(Manifest);
				if (query == null) return;

				var content = new PresetContentDto
				{
					Query = query
				};
				content.Grid.HiddenColumns = ColumnVisibility
					.Where(c => !c.IsVisible)
					.Select(c => c.Key)
					.ToList();
				content.Grid.Order = ColumnOrder.Count > 0
					? [.. ColumnOrder]
                    : ColumnVisibility.Select(c => c.Key).ToList();

				var preset = new PresetDto
				{
					PresetId = Guid.Empty,
					ReportKey = ReportKey,
					Name = string.IsNullOrWhiteSpace(NewPresetName) ? "Můj pohled" : NewPresetName,
					IsSystem = false,
					Content = content
				};

				var savedId = _svc.SavePreset(new SavePresetRequestDto { Preset = preset, UserId = UserId });
				StatusText = "Preset uložen: " + savedId;

				Presets.Clear();
				foreach (var p in _svc.GetPresets(ReportKey, UserId))
					Presets.Add(p);
			}
			catch (Exception ex)
			{
				StatusText = "SavePreset error: " + ex.Message;
			}
		}

		private void OverwritePreset()
		{
			if (Manifest == null) return;
			if (SelectedPreset == null)
			{
				StatusText = "OverwritePreset error: Není vybraný preset.";
				return;
			}
			if (SelectedPreset.IsSystem)
			{
				StatusText = "OverwritePreset error: Systémový preset nelze přepsat.";
				return;
			}

			try
			{
				var query = BuildQuerySpec(Manifest);
				if (query == null) return;

				var content = new PresetContentDto
				{
					Query = query
				};
				content.Grid.HiddenColumns = ColumnVisibility
					.Where(c => !c.IsVisible)
					.Select(c => c.Key)
					.ToList();
				content.Grid.Order = ColumnOrder.Count > 0
					? [.. ColumnOrder]
					: ColumnVisibility.Select(c => c.Key).ToList();

				var preset = new PresetDto
				{
					PresetId = SelectedPreset.PresetId,
					ReportKey = ReportKey,
					Name = SelectedPreset.Name,
					IsSystem = false,
					Content = content
				};

				var savedId = _svc.SavePreset(new SavePresetRequestDto { Preset = preset, UserId = UserId });
				StatusText = "Preset přepsán: " + savedId;

				Presets.Clear();
				foreach (var p in _svc.GetPresets(ReportKey, UserId))
					Presets.Add(p);
			}
			catch (Exception ex)
			{
				StatusText = "OverwritePreset error: " + ex.Message;
			}
		}

		private bool AddVisibleFilters(QuerySpecDto q)
		{
			foreach (var c in Conditions)
			{
				if (c.SelectedColumn == null) continue;

				if (!c.TryGetValuesForDto(out var values, out var error))
				{
					StatusText = "Query validation error: " + error;
					return false;
				}

				var f = new FilterSpecDto
				{
					ColumnKey = c.SelectedColumn.Key,
					Operation = c.SelectedOp,
					Values = values
				};
				q.Filters.Add(f);
			}

			return true;
		}

		private void AddHiddenFilters(QuerySpecDto q)
		{
			if (Manifest == null)
			{
				return;
			}

			foreach (var hidden in HiddenFilters)
			{
				var col = Manifest.Columns.FirstOrDefault(x => x.Key.Equals(hidden.ColumnKey, StringComparison.OrdinalIgnoreCase));
				if (col == null || !col.FilterEnabled || !col.FilterHidden)
				{
					continue;
				}

				q.Filters.Add(new FilterSpecDto
				{
					ColumnKey = hidden.ColumnKey,
					Operation = hidden.Operation,
					Values = hidden.Values ?? []
				});
			}
		}

		private void AddVisibleSorts(QuerySpecDto q)
		{
			foreach (var s in Sorts)
			{
				if (s.SelectedColumn == null) continue;
				q.Sorting.Add(new SortSpecDto { ColumnKey = s.SelectedColumn.Key, Direction = s.SelectedDirection });
			}
		}

		private void AddHiddenSorts(QuerySpecDto q)
		{
			if (Manifest == null)
			{
				return;
			}

			foreach (var hidden in HiddenSorts)
			{
				var col = Manifest.Columns.FirstOrDefault(x => x.Key.Equals(hidden.ColumnKey, StringComparison.OrdinalIgnoreCase));
				if (col == null || !col.SortEnabled || !col.SortHidden)
				{
					continue;
				}

				q.Sorting.Add(new SortSpecDto
				{
					ColumnKey = hidden.ColumnKey,
					Direction = hidden.Direction
				});
			}
        }

        #endregion

        #region Classes

        public sealed class ColumnVisibilityItem : NotificationObject
        {
            public required string Key { get; set => SetValue(ref field, value); }
            public required string DisplayName { get; set => SetValue(ref field, value); }
            public bool IsVisible { get; set => SetValue(ref field, value); }
        }

        #endregion
    }
}
