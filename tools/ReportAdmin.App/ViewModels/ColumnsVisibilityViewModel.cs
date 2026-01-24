using ReportAdmin.App.Messages;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ColumnsVisibilityViewModel : DataEditorVM<ObservableCollection<string>>
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

            RegisterMessage<ColumnChangedMessage>(OnColumnChanged);
        }

        #endregion

        #region Properties

        public ObservableCollection<ColumnVisibilityViewModel> Columns { get; } = [];

        #endregion

        #region Commands

        public RelayCommand ShowAllColumnsCommand { get; }
        public RelayCommand HideAllColumnsCommand { get; }

        #endregion

        #region Override Methods

        protected override void OnGetData(ObservableCollection<string> data)
        {
            foreach (var column in Columns)
            {
                if (!column.IsVisible)
                {
                    data.Add(column.Column.Key);
                }
            }
        }

        protected override void OnSetData(ObservableCollection<string> data)
        {
            Columns.Clear();

            var hidden = new HashSet<string>(data, StringComparer.OrdinalIgnoreCase);

            var msg = SendMessage<GetColumnsMessage>();
            foreach (var column in msg.Columns)
            {
                if (column.Hidden)
                {
                    continue;
                }

                Columns.Add(new ColumnVisibilityViewModel
                {
                    Column = column,
                    IsVisible = !hidden.Contains(column.Key)
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
        }
        #endregion
    }
}
