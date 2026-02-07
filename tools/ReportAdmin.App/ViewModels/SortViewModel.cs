using ReportAdmin.App.Messages;
using ReportManager.DefinitionModel.Models;
using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class SortViewModel : DataEditorVM<List<SortSpecJson>>
    {
        #region Ctor

        public SortViewModel()
        {
            AddSortCommand = new RelayCommand(AddSort);
            RemoveSortCommand = new RelayCommand(RemoveSort, () => SelectedSort != null);
            MoveSortUpCommand = new RelayCommand(() => MoveSort(-1), () => CanMoveSort(-1));
            MoveSortDownCommand = new RelayCommand(() => MoveSort(1), () => CanMoveSort(1));

            RegisterMessage<ColumnChangedMessage>(OnColumnChanged);
        }

        #endregion

        #region Properties

        public ObservableCollection<SortDirection> SortDirectionValues { get; } = new(Enum.GetValues(typeof(SortDirection)).Cast<SortDirection>());
        public ObservableCollection<IColumn> SortableColumns { get; } = [];
        public ObservableCollection<SortRuleVm> Sorting { get; } = [];
        public SortRuleVm? SelectedSort { get; set => SetValue(ref field, value, OnSelectedSortChanged); }

        #endregion

        #region Commands

        public RelayCommand AddSortCommand { get; }
        public RelayCommand RemoveSortCommand { get; }
        public RelayCommand MoveSortUpCommand { get; }
        public RelayCommand MoveSortDownCommand { get; }

        #endregion

        #region Overrides

        protected override void OnGetData(List<SortSpecJson> data)
        {
            foreach (var s in Sorting)
            {
                if (s.Column == null || string.IsNullOrWhiteSpace(s.Column.Key))
                {
                    continue;
                }

                data.Add(new SortSpecJson() { ColumnKey = s.Column.Key, Direction = s.Direction });
            }
        }

        protected override void OnSetData(List<SortSpecJson> data)
        {
            SortableColumns.Clear();
            Sorting.Clear();

            var msg = SendMessage<GetColumnsMessage>();
            foreach (var col in msg.Columns.Where(c => c.Sortable && c.Sort?.Hidden == false))
                SortableColumns.Add(col);

            foreach (var s in data)
                Sorting.Add(new SortRuleVm { Column = msg.Columns.Find(x => x.Key == s.ColumnKey), Direction = s.Direction });

            RaiseCanExec();
        }

        #endregion

        #region Private Methods

        private void AddSort()
        {
            var first = SortableColumns.FirstOrDefault();
            Sorting.Add(new SortRuleVm { Column = first, Direction = SortDirection.Asc });
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

        private bool CanMoveSort(int delta)
        {
            if (SelectedSort == null) return false;

            var currentIndex = Sorting.IndexOf(SelectedSort);
            if (currentIndex == -1) return false;

            var targetIndex = currentIndex + delta;
            return targetIndex >= 0 && targetIndex < Sorting.Count;
        }

        private void OnColumnChanged(ColumnChangedMessage message)
        {
            switch (message.ChangeKind)
            {
                case ColumnChangeKind.Added:
                    if (message.Column.Sortable) SortableColumns.Add(message.Column);
                    break;
                case ColumnChangeKind.Deleted:
                    if (message.Column.Sortable)
                    {
                        foreach (var sort in Sorting.Where(x => x.Column == message.Column).ToList())
                        {
                            Sorting.Remove(sort);
                        }
                        SortableColumns.Remove(message.Column);
                    }
                    break;
                case ColumnChangeKind.Changed:
                    var pv = message.PropertyValue;
                    if (pv?.Property == ColumnProperty.Sortable)
                    {
                        if (message.Column.Sortable)
                        {
                            SortableColumns.Add(message.Column);
                        }
                        else
                        {
                            foreach (var sort in Sorting.Where(x => x.Column == message.Column).ToList())
                            {
                                Sorting.Remove(sort);
                            }
                            SortableColumns.Remove(message.Column);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void OnSelectedSortChanged(SortRuleVm selectedSort)
        {
            RaiseCanExec();
        }

        #endregion
    }
}
