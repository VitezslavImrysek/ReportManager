using ReportManager.Shared.Dto;
using ReportManager.Client.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ReportManager.Client
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<string, DataGridColumn> _gridColumnsByKey = new Dictionary<string, DataGridColumn>(StringComparer.OrdinalIgnoreCase);

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            DataContext = vm;

            vm.PropertyChanged += VmOnPropertyChanged;
            vm.ColumnVisibility.CollectionChanged += (_, __) => WireColumnVisibility(vm);
            WireColumnVisibility(vm);

            // pokud je manifest už načtený v konstruktoru VM, postav sloupce hned
            if (vm.Manifest != null)
                BuildColumns(vm.Manifest);
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (e.PropertyName == nameof(MainViewModel.Manifest))
            {
                var vm = (MainViewModel)sender;
                if (vm.Manifest != null)
                    BuildColumns(vm.Manifest);
            }
        }

        private void BuildColumns(ReportManifestDto manifest)
        {
            _gridColumnsByKey.Clear();
            ReportGrid.Columns.Clear();

            foreach (var col in manifest.Columns)
            {
                // 1) respektuj Hidden
                if (col.Hidden)
                    continue;

                // 2) typově zvol editor/column type
                var gridCol = new DataGridTextColumn
                {
                    Header = col.DisplayName,
                    Binding = new Binding($"[{col.Key}]"),
                    IsReadOnly = true,
                    CanUserSort = col.SortEnabled
                };

                // 3) typové formátování
                ApplyFormatting(gridCol, col.Type);

                ReportGrid.Columns.Add(gridCol);
                _gridColumnsByKey[col.Key] = gridCol;
            }
        }

        private void WireColumnVisibility(MainViewModel vm)
        {
            foreach (var item in vm.ColumnVisibility)
            {
                item.PropertyChanged -= ColumnVisibilityItemOnPropertyChanged;
                item.PropertyChanged += ColumnVisibilityItemOnPropertyChanged;
            }

            ApplyColumnVisibility(vm);
        }

        private void ColumnVisibilityItemOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.ColumnVisibilityItem.IsVisible))
            {
                if (DataContext is MainViewModel vm)
                    ApplyColumnVisibility(vm);
            }
        }

        private void ApplyColumnVisibility(MainViewModel vm)
        {
            // default: vše viditelné (včetně alwaysSelect sloupců – ty tu nejsou v listu)
            foreach (var kv in _gridColumnsByKey)
                kv.Value.Visibility = Visibility.Visible;

            // skryj pouze to, co user vypnul (jen non-hidden & non-alwaysSelect sloupce)
            foreach (var item in vm.ColumnVisibility)
            {
                if (_gridColumnsByKey.TryGetValue(item.Key, out var col))
                    col.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private static void ApplyFormatting(DataGridTextColumn col, ReportColumnType type)
        {
            // Nastav podle potřeby – je to jen základ
            switch (type)
            {
                case ReportColumnType.Date:
                    col.Binding.StringFormat = "d";      // krátké datum
                    break;

                case ReportColumnType.DateTime:
                    col.Binding.StringFormat = "g";      // datum + čas (short)
                    break;

                case ReportColumnType.Decimal:
                    col.Binding.StringFormat = "N2";     // 2 desetinná místa
                    break;

                case ReportColumnType.Double:
                    col.Binding.StringFormat = "N3";
                    break;

                default:
                    // bez formátu
                    break;
            }
        }
    }
}
