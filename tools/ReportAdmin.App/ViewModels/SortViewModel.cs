using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models;
using ReportAdmin.Core.Models.Definition;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class SortViewModel : DataEditorVM<ObservableCollection<SortSpecUi>>
    {
        public SortViewModel()
        {
            AddSortCommand = new RelayCommand(AddSort);
            RemoveSortCommand = new RelayCommand(RemoveSort, () => SelectedSort != null);
            MoveSortUpCommand = new RelayCommand(() => MoveSort(-1), () => SelectedSort != null);
            MoveSortDownCommand = new RelayCommand(() => MoveSort(1), () => SelectedSort != null);
        }

        public ObservableCollection<SortDirection> SortDirectionValues { get; } = new(Enum.GetValues(typeof(SortDirection)).Cast<SortDirection>());
        public ObservableCollection<ReportColumnUi> SortableColumns { get; } = new();
        public ObservableCollection<SortRuleVm> Sorting { get; } = [];
        public SortRuleVm? SelectedSort { get; set => SetValue(ref field, value); }

        public RelayCommand AddSortCommand { get; }
        public RelayCommand RemoveSortCommand { get; }
        public RelayCommand MoveSortUpCommand { get; }
        public RelayCommand MoveSortDownCommand { get; }

        protected override void OnGetData(ObservableCollection<SortSpecUi> data)
        {
            foreach (var s in Sorting)
            {
                if (string.IsNullOrWhiteSpace(s.ColumnKey))
                {
                    continue;
                }

                data.Add(new SortSpecUi() { ColumnKey = s.ColumnKey, Direction = s.Direction });
            }
        }

        protected override void OnSetData(ObservableCollection<SortSpecUi> data)
        {
            SortableColumns.Clear();
            Sorting.Clear();

            var msg = SendMessage<GetColumnsMessage>();
            foreach (var col in msg.Columns.Where(c => c.Sortable && c.Sort?.Hidden == false))
                SortableColumns.Add(col);

            foreach (var s in data)
                Sorting.Add(new SortRuleVm { ColumnKey = s.ColumnKey, Direction = s.Direction });
        }

        private void AddSort()
        {
            var first = SortableColumns.FirstOrDefault();
            Sorting.Add(new SortRuleVm { ColumnKey = first?.Key ?? "", Direction = SortDirection.Asc });
            SelectedSort = Sorting.LastOrDefault();
            RaiseCanExec();
        }

        private void RemoveSort()
        {
            if (SelectedSort == null) return;
            Sorting.Remove(SelectedSort);
            SelectedSort = Sorting.LastOrDefault();
            RaiseCanExec();
        }

        private void MoveSort(int delta)
        {
            if (SelectedSort == null) return;
            var idx = Sorting.IndexOf(SelectedSort);
            var nidx = idx + delta;
            if (nidx < 0 || nidx >= Sorting.Count) return;
            Sorting.Move(idx, nidx);
            RaiseCanExec();
        }

        private void RaiseCanExec()
        {
            AddSortCommand.RaiseCanExecuteChanged();
            RemoveSortCommand.RaiseCanExecuteChanged();
            MoveSortUpCommand.RaiseCanExecuteChanged();
            MoveSortDownCommand.RaiseCanExecuteChanged();
        }
    }
}
