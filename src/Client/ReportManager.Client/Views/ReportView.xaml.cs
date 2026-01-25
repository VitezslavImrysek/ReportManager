using ReportManager.Client.ViewModels;
using ReportManager.Shared.Dto;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Linq;

namespace ReportManager.Client.Views
{
    /// <summary>
    /// Interaction logic for ReportView.xaml
    /// </summary>
    public partial class ReportView : UserControl
    {
        private readonly Dictionary<string, DataGridColumn> _gridColumnsByKey = new Dictionary<string, DataGridColumn>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<DataGridColumn, string> _gridColumnKeys = new Dictionary<DataGridColumn, string>();

        public ReportView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        protected virtual DataGridColumn OnBuildVirtualColumn(ReportColumnManifestDto reportColumn)
        {
            // výchozí implementace – stejné jako běžný sloupec
            var column = new DataGridTemplateColumn();

            var cellTemplate = TryFindResource($"{reportColumn.Key}_CellTemplate");
            if (cellTemplate is DataTemplate dt)
            {
                column.CellTemplate = dt;
            }
            else
            {
                // fallback for debugging
                column.Header = reportColumn.DisplayName;
            }

            return column;
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
            else if (e.PropertyName == nameof(ReportViewModel.ColumnOrder))
            {
                var vm = (ReportViewModel)sender;
                ApplyColumnOrder(vm);
            }
        }

        private void BuildColumns(ReportManifestDto manifest)
        {
            _gridColumnsByKey.Clear();
            _gridColumnKeys.Clear();
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
                _gridColumnKeys[gridColumn] = reportColumn.Key;
            }

            if (DataContext is ReportViewModel vm)
            {
                if (vm.ColumnOrder.Count > 0)
                {
                    ApplyColumnOrder(vm);
                }
                else
                {
                    UpdateColumnOrderFromGrid(vm);
                }

                ApplyColumnVisibility(vm);
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

        private void ApplyColumnOrder(ReportViewModel vm)
        {
            if (_gridColumnsByKey.Count == 0)
            {
                return;
            }

            if (vm.ColumnOrder.Count == 0)
            {
                return;
            }

            var orderedColumns = new List<DataGridColumn>();
            foreach (var key in vm.ColumnOrder)
            {
                if (_gridColumnsByKey.TryGetValue(key, out var column))
                {
                    orderedColumns.Add(column);
                }
            }

            var remaining = ReportGrid.Columns
                .Except(orderedColumns)
                .OrderBy(column => column.DisplayIndex);
            orderedColumns.AddRange(remaining);

            for (var i = 0; i < orderedColumns.Count; i++)
            {
                orderedColumns[i].DisplayIndex = i;
            }
        }

        private void UpdateColumnOrderFromGrid(ReportViewModel vm)
        {
            var order = ReportGrid.Columns
                .OrderBy(column => column.DisplayIndex)
                .Select(column => _gridColumnKeys.TryGetValue(column, out var key) ? key : null)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key!)
                .ToList();

            if (order.Count > 0)
            {
                vm.ColumnOrder = order;
            }
        }

        private void ReportGrid_OnColumnReordered(object sender, DataGridColumnEventArgs e)
        {
            if (DataContext is ReportViewModel vm)
            {
                UpdateColumnOrderFromGrid(vm);
            }
        }

        private DataGridColumn BuildColumn(ReportColumnManifestDto reportColumn)
        {
            if (reportColumn.Virtual)
            {
                return OnBuildVirtualColumn(reportColumn);
            }

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
