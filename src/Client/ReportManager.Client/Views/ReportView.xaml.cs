using ReportManager.Client.ViewModels;
using ReportManager.Shared.Dto;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ReportManager.Client.Views
{
    /// <summary>
    /// Interaction logic for ReportView.xaml
    /// </summary>
    public partial class ReportView : UserControl
    {
        private readonly Dictionary<string, DataGridColumn> _gridColumnsByKey = new Dictionary<string, DataGridColumn>(StringComparer.OrdinalIgnoreCase);

        public ReportView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ReportViewModel oldVM)
            {
                oldVM.PropertyChanged -= VmOnPropertyChanged;
                oldVM.ColumnVisibility.CollectionChanged -= (_, __) => WireColumnVisibility(oldVM);
            }

            if (e.NewValue is ReportViewModel vm)
            {
                vm.PropertyChanged += VmOnPropertyChanged;
                vm.ColumnVisibility.CollectionChanged += (_, __) => WireColumnVisibility(vm);
                WireColumnVisibility(vm);

                // pokud je manifest už načtený v konstruktoru VM, postav sloupce hned
                if (vm.Manifest != null)
                    BuildColumns(vm.Manifest);
            }
        }

        private void VmOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender == null)
            {
                return;
            }

            if (e.PropertyName == nameof(ReportViewModel.Manifest))
            {
                var vm = (ReportViewModel)sender;
                if (vm.Manifest != null)
                    BuildColumns(vm.Manifest);
            }
        }

        private void BuildColumns(ReportManifestDto manifest)
        {
            _gridColumnsByKey.Clear();
            ReportGrid.Columns.Clear();

            foreach (var reportColumn in manifest.Columns)
            {
                // respektuj Hidden
                if (reportColumn.Hidden)
                    continue;

                // typově zvol editor/column type
                var gridColumn = BuildColumn(reportColumn);

                ReportGrid.Columns.Add(gridColumn);
                _gridColumnsByKey[reportColumn.Key] = gridColumn;
            }
        }

        private void WireColumnVisibility(ReportViewModel vm)
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
            if (e.PropertyName == nameof(ReportViewModel.ColumnVisibilityItem.IsVisible))
            {
                if (DataContext is ReportViewModel vm)
                    ApplyColumnVisibility(vm);
            }
        }

        private void ApplyColumnVisibility(ReportViewModel vm)
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

        private static DataGridBoundColumn BuildColumn(ReportColumnManifestDto reportColumn)
        {
            DataGridBoundColumn column;

            switch (reportColumn.Type)
            {
                case ReportColumnType.Boolean:
                    column = new DataGridCheckBoxColumn();
                    break;
                case ReportColumnType.Integer:
                case ReportColumnType.Long:
                case ReportColumnType.Decimal:
                case ReportColumnType.Double:
                case ReportColumnType.String:
                case ReportColumnType.Date:
                case ReportColumnType.DateTime:
                case ReportColumnType.Guid:
                default:
                    column = new DataGridTextColumn();
                    break;
            }

            column.Header = reportColumn.DisplayName;
            column.Binding = new Binding($"[{reportColumn.Key}]");
            column.IsReadOnly = true;

            // typové formátování
            ApplyFormatting(column, reportColumn.Type);

            return column;
        }

        private static void ApplyFormatting(DataGridBoundColumn col, ReportColumnType type)
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
