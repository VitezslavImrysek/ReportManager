using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
    public class SortSpecsViewModel : DataEditorVM<List<SortSpecDto>>
    {
        #region Ctor

        public SortSpecsViewModel()
        {
            AddSortCommand = new RelayCommand(AddSort);
        }

        #endregion

        #region Properties

        public ObservableCollection<ColumnOption> AvailableColumns { get; init; } = [];
        public ObservableCollection<SortSpecViewModel> Sorts { get; } = [];
        public ObservableCollection<SortSpecDto> HiddenSorts { get; } = [];

        #endregion

        #region Commands

        public ICommand AddSortCommand { get; }

        #endregion

        #region Overrides
        

        protected override void OnGetData(List<SortSpecDto> data)
        {
            foreach (var s in Sorts)
            {
                if (s.SelectedColumn == null) continue;
                data.Add(new SortSpecDto { ColumnKey = s.SelectedColumn.Key, Direction = s.SelectedDirection });
            }

            foreach (var hidden in HiddenSorts)
            {
                var col = AvailableColumns.FirstOrDefault(x => x.Key.Equals(hidden.ColumnKey, StringComparison.OrdinalIgnoreCase));
                if (col == null || !col.CanSort || !col.SortHidden)
                {
                    continue;
                }

                data.Add(new SortSpecDto
                {
                    ColumnKey = hidden.ColumnKey,
                    Direction = hidden.Direction
                });
            }
        }

        protected override void OnSetData(List<SortSpecDto> data)
        {
            Sorts.Clear();
            HiddenSorts.Clear();
            foreach (var s in data)
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
                    AvailableColumnItems = GetSortableColumnItems(),
                    SelectedDirection = s.Direction
                };
                vm.SelectColumn(col);
                vm.RemoveCommand = new RelayCommand(() => Sorts.Remove(vm));
                Sorts.Add(vm);
            }
        }

        #endregion

        #region Private methods

        private void AddSort()
        {
            if (AvailableColumns.Count == 0) return;
            var availableColumnItems = GetSortableColumnItems();
            var firstColumn = availableColumnItems.FirstOrDefault(x => x.IsSelectable)?.Column;
            if (firstColumn == null)
            {
                return;
            }

            var vm = new SortSpecViewModel
            {
                AvailableColumnItems = availableColumnItems,
                SelectedDirection = SortDirection.Asc
            };
            vm.SelectColumn(firstColumn);
            vm.RemoveCommand = new RelayCommand(() => Sorts.Remove(vm));
            Sorts.Add(vm);
        }

        private ObservableCollection<ColumnPickerItem> GetSortableColumnItems()
        {
            return ColumnPickerFactory.Build(AvailableColumns.Where(x => x.CanSort && !x.SortHidden));
        }

        #endregion
    }
}
