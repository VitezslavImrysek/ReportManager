using ReportManager.Client.Views;
using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
    public class QueryConditionsViewModel : DataEditorVM<List<FilterSpecDto>>
    {
        #region Ctor

        public QueryConditionsViewModel()
        {
            AddConditionCommand = new RelayCommand(AddCondition);
        }

        #endregion

        #region Properties

        public ObservableCollection<ColumnOption> AvailableColumns { get; init; } = [];
        public ObservableCollection<FilterSpecDto> HiddenFilters { get; } = [];
        public ObservableCollection<QueryConditionViewModel> Conditions { get; } = [];

        #endregion

        #region Commands

        public ICommand AddConditionCommand { get; }

        #endregion

        #region Overrides

        protected override void OnGetData(List<FilterSpecDto> data)
        {
            foreach (var c in Conditions)
            {
                if (c.SelectedColumn == null) continue;

                c.TryGetValuesForDto(out var values, out _);

                var f = new FilterSpecDto
                {
                    ColumnKey = c.SelectedColumn.Key,
                    Operation = c.SelectedOp,
                    Values = values
                };
                data.Add(f);
            }

            foreach (var hidden in HiddenFilters)
            {
                var col = AvailableColumns.FirstOrDefault(x => x.Key.Equals(hidden.ColumnKey, StringComparison.OrdinalIgnoreCase));
                if (col == null || !col.CanFilter || !col.FilterHidden)
                {
                    continue;
                }

                data.Add(new FilterSpecDto
                {
                    ColumnKey = hidden.ColumnKey,
                    Operation = hidden.Operation,
                    Values = hidden.Values ?? []
                });
            }
        }

        protected override void OnSetData(List<FilterSpecDto> data)
        {
            Conditions.Clear();
            HiddenFilters.Clear();

            foreach (var f in data)
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
                    AvailableColumns = GetFilterableColumns()
                };
                vm.SelectColumn(col);
                vm.SelectedOp = f.Operation;
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
        }

        protected override bool OnValidate(StringBuilder log)
        {
            var isOK = base.OnValidate(log);

            foreach (var c in Conditions)
            {
                if (c.SelectedColumn == null) continue;

                if (!c.TryGetValuesForDto(out var values, out var error))
                {
                    log.AppendLine("Query validation error: " + error);
                    isOK = false;
                }
            }

            return isOK;
        }

        #endregion

        #region Private Methods

        private void AddCondition()
        {
            if (AvailableColumns.Count == 0) return;
            var availableColumns = GetFilterableColumns();

            var dialogVm = new ColumnPickerDialogViewModel(availableColumns, null);
            var dialog = new ColumnPickerDialog
            {
                Owner = Application.Current?.MainWindow,
                DataContext = dialogVm
            };

            if (dialog.ShowDialog() != true || dialogVm.SelectedColumn == null)
            {
                return;
            }

            var vm = new QueryConditionViewModel
            {
                AvailableColumns = availableColumns
            };
            vm.SelectColumn(dialogVm.SelectedColumn);
            vm.RemoveCommand = new RelayCommand(() => Conditions.Remove(vm));
            Conditions.Add(vm);
        }

        private ObservableCollection<ColumnOption> GetFilterableColumns()
        {
            return new ObservableCollection<ColumnOption>(AvailableColumns.Where(x => x.CanFilter && !x.FilterHidden));
        }

        #endregion
    }
}
