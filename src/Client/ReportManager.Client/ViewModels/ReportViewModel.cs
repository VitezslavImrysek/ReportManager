using ReportManager.Client.Extensions;
using ReportManager.Lib.Wpf;
using ReportManager.Proxy.Services;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.Data;
using System.ServiceModel;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
	public sealed class ReportViewModel : ViewModelBase
    {
        #region Private Fields

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

		public QueryConditionsViewModel? ConditionsVM { get; private set => SetValue(ref field, value); }
        public SortSpecsViewModel? SortSpecsVM { get; private set => SetValue(ref field, value); }

        public ObservableCollection<PresetInfoDto> Presets { get; } = [];
        public PresetInfoDto? SelectedPreset { get; set => SetValue(ref field, value); }
        public string NewPresetName { get; set; } = "Můj pohled";

        #endregion

        #region Commands

        public ICommand LoadManifestCommand { get; }
        public ICommand QueryCommand { get; }
        public ICommand ClearServerQueryCommand { get; }
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
				RefreshPresets();

				// reset conditions/sorts
				ConditionsVM = new QueryConditionsViewModel() { AvailableColumns = AvailableColumns };
				ConditionsVM.SetData([]);	// TODO: Add hidden filter

				SortSpecsVM = new SortSpecsViewModel() { AvailableColumns = AvailableColumns };
				SortSpecsVM.SetData([]);    // TODO: Add hidden sorts

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
			// TODO: Validate
			//if (!ConditionsViewModel.Validate())
			//{
			//	return null;
			//}

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

			ConditionsVM.GetData(q.Filters);
			SortSpecsVM.GetData(q.Sorting);

			// Selected columns: use currently visible columns from manifest defaults (empty => server decides)
			return q;
		}

		private void ClearServerQuery()
		{
			ConditionsVM.SetData([]);
			SortSpecsVM.SetData([]);
            _pageIndex = 0;

			Query();
		}

		private void LoadPreset()
		{
			try
			{
				if (SelectedPreset == null) return;

				var preset = _svc.GetPreset(SelectedPreset.PresetId, UserId);
				var content = preset.Content ?? new PresetContentDto();

				// 1) apply query
				ConditionsVM.SetData(content.Query.Filters ?? []);
                SortSpecsVM.SetData(content.Query.Sorting ?? []);

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
                var savedId = SavePreset(Manifest, Guid.Empty, string.IsNullOrWhiteSpace(NewPresetName) ? "Můj pohled" : NewPresetName);
                if (savedId == null)
                {
                    return;
                }

                StatusText = "Preset uložen: " + savedId;
                RefreshPresets();
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
				var savedId = SavePreset(Manifest, SelectedPreset.PresetId, SelectedPreset.Name);
				if (savedId == null)
				{
					return;
				}

				StatusText = "Preset přepsán: " + savedId;
                RefreshPresets();
            }
			catch (Exception ex)
			{
				StatusText = "OverwritePreset error: " + ex.Message;
			}
		}

		private Guid? SavePreset(ReportManifestDto manifest, Guid presetId, string presetName)
		{
            var query = BuildQuerySpec(manifest);
            if (query == null) return null;

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
                PresetId = presetId,
                ReportKey = ReportKey,
                Name = presetName,
                IsSystem = false,
                Content = content
            };

            var savedId = _svc.SavePreset(new SavePresetRequestDto { Preset = preset, UserId = UserId });
			return savedId;
        }

        private void RefreshPresets()
        {
            Presets.Clear();
            foreach (var p in _svc.GetPresets(ReportKey, UserId))
                Presets.Add(p);
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
