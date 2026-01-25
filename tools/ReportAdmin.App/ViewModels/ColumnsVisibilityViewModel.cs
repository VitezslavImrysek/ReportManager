using ReportAdmin.App.Messages;
using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportAdmin.App.ViewModels
{
    public class ColumnsVisibilityViewModel : DataEditorVM<GridStateJson>
    {
        #region Ctor

        public ColumnsVisibilityViewModel()
        {
            ShowAllColumnsCommand = new RelayCommand(() =>
            {
                foreach (var c in Columns)
                    c.IsVisible = true;
            });

            HideAllColumnsCommand = new RelayCommand(() =>
            {
                foreach (var c in Columns)
                    c.IsVisible = false;
            });

            MoveColumnUpCommand = new RelayCommand(MoveSelectedColumnUp, CanMoveSelectedColumnUp);
            MoveColumnDownCommand = new RelayCommand(MoveSelectedColumnDown, CanMoveSelectedColumnDown);

            RegisterMessage<ColumnChangedMessage>(OnColumnChanged);
        }

        #endregion

        #region Properties

        public ObservableCollection<ColumnVisibilityViewModel> Columns { get; } = [];
        public ColumnVisibilityViewModel? SelectedColumn { get; set => SetValue(ref field, value, _ => RaiseCanExec()); }

        #endregion

        #region Commands

        public RelayCommand ShowAllColumnsCommand { get; }
        public RelayCommand HideAllColumnsCommand { get; }
        public RelayCommand MoveColumnUpCommand { get; }
        public RelayCommand MoveColumnDownCommand { get; }

        #endregion

        #region Override Methods

        protected override void OnGetData(GridStateJson data)
        {
            data.HiddenColumns.Clear();
            data.Order.Clear();

            foreach (var column in Columns)
            {
                data.Order.Add(column.Column.Key);

                if (!column.IsVisible)
                {
                    data.HiddenColumns.Add(column.Column.Key);
                }
            }
        }

        protected override void OnSetData(GridStateJson data)
        {
            Columns.Clear();

            var hidden = new HashSet<string>(data.HiddenColumns ?? [], StringComparer.OrdinalIgnoreCase);
            var orderLookup = (data.Order ?? [])
                .Select((key, index) => new { key, index })
                .ToDictionary(x => x.key, x => x.index, StringComparer.OrdinalIgnoreCase);

            var msg = SendMessage<GetColumnsMessage>();
            var columns = msg.Columns
                .Select((column, index) => new { column, index })
                .Where(x => !x.column.Hidden)
                .OrderBy(x => orderLookup.TryGetValue(x.column.Key, out var orderIndex) ? orderIndex : int.MaxValue)
                .ThenBy(x => x.index);

            foreach (var entry in columns)
            {
                Columns.Add(new ColumnVisibilityViewModel
                {
                    Column = entry.column,
                    IsVisible = !hidden.Contains(entry.column.Key)
                });
            }

            RaiseCanExec();
        }

        #endregion

        #region Private Methods
        
        private void RaiseCanExec()
        {
            ShowAllColumnsCommand.RaiseCanExecuteChanged();
            HideAllColumnsCommand.RaiseCanExecuteChanged();
            MoveColumnUpCommand.RaiseCanExecuteChanged();
            MoveColumnDownCommand.RaiseCanExecuteChanged();
        }

        private bool CanMoveSelectedColumnUp()
            => SelectedColumn != null && Columns.IndexOf(SelectedColumn) > 0;

        private bool CanMoveSelectedColumnDown()
            => SelectedColumn != null && Columns.IndexOf(SelectedColumn) >= 0 && Columns.IndexOf(SelectedColumn) < Columns.Count - 1;

        private void MoveSelectedColumnUp()
        {
            if (SelectedColumn == null) return;
            var index = Columns.IndexOf(SelectedColumn);
            if (index <= 0) return;
            Columns.Move(index, index - 1);
            RaiseCanExec();
        }

        private void MoveSelectedColumnDown()
        {
            if (SelectedColumn == null) return;
            var index = Columns.IndexOf(SelectedColumn);
            if (index < 0 || index >= Columns.Count - 1) return;
            Columns.Move(index, index + 1);
            RaiseCanExec();
        }

        private void OnColumnChanged(ColumnChangedMessage message)
        {
            switch (message.ChangeKind)
            {
                case ColumnChangeKind.Added:
                    if (!message.Column.Hidden) Columns.Add(new ColumnVisibilityViewModel() { Column = message.Column, IsVisible = true });
                    break;
                case ColumnChangeKind.Deleted:
                    if (!message.Column.Hidden)
                    {
                        foreach (var column in Columns.Where(x => x.Column == message.Column).ToList())
                        {
                            Columns.Remove(column);
                        }
                    }
                    break;
                case ColumnChangeKind.Changed:
                    var pv = message.PropertyValue;
                    if (pv?.Property == ColumnProperty.Hidden)
                    {
                        if (message.Column.Hidden)
                        {
                            Columns.Add(new ColumnVisibilityViewModel() { Column = message.Column, IsVisible = true });
                        }
                        else
                        {
                            foreach (var column in Columns.Where(x => x.Column == message.Column).ToList())
                            {
                                Columns.Remove(column);
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            RaiseCanExec();
        }
        #endregion
    }
}
