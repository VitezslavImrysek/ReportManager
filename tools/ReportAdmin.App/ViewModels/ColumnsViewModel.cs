using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
using ReportAdmin.Core.Db;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ColumnsViewModel : DataEditorVM<List<ReportColumnJson>>
    {
        #region Ctor

        public ColumnsViewModel()
        {
            AddColumnCommand = new RelayCommand(AddColumn);
            RemoveSelectedColumnCommand = new RelayCommand(RemoveSelectedColumn);
            MoveUpCommand = new RelayCommand(MoveUp, CanMoveUp);
            MoveDownCommand = new RelayCommand(MoveDown, CanMoveDown);

            RegisterMessage<GetColumnsMessage>(OnGetColumnsMessageReceived);
        }

        #endregion

        #region Properties

        public ObservableCollection<ReportColumnType> ColumnTypeValues { get; } = new(Enum.GetValues(typeof(ReportColumnType)).Cast<ReportColumnType>());
        public ObservableCollection<ColumnViewModel> Columns { get; set => SetValue(ref field, value); } = [];
        public ColumnViewModel? SelectedColumn { get; set => SetValue(ref field, value, OnSelectedColumnChanged); }

        #endregion

        #region Commands

        public RelayCommand AddColumnCommand { get; }
        public RelayCommand RemoveSelectedColumnCommand { get; }
        public RelayCommand MoveUpCommand { get; }
        public RelayCommand MoveDownCommand { get; }

        #endregion

        #region Public Methods

        public void UpdateColumns(List<DbIntrospector.ViewColumn> cols)
        {
            // Delete non-virtual columns which are not present in the imported view
            var existingColumnKeys = cols.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                if (col.Virtual)
                {
                    continue;
                }

                if (!existingColumnKeys.Contains(col.Key))
                {
                    Columns.RemoveAt(i);
                    i--;
                    SendMessage(new ColumnChangedMessage(col, ColumnChangeKind.Deleted));
                }
            }

            // Add or update imported columns
            foreach (var col in cols)
            {
                var oldColumn = Columns.FirstOrDefault(c => string.Equals(c.Key, col.Name, StringComparison.OrdinalIgnoreCase));
                if (oldColumn != null)
                {
                    // Update type if changed
                    var newType = DbIntrospector.MapSqlType(col.SqlType);
                    if (oldColumn.Type != newType)
                        oldColumn.Type = newType;
                }
                else
                {
                    var type = DbIntrospector.MapSqlType(col.SqlType);
                    var textKey = KnownTextKeys.GetColumnHeaderKey(col.Name);

                    var ui = new ReportColumnJson
                    {
                        Key = col.Name,
                        Type = type,
                    };

                    var vm = new ColumnViewModel()
                    {
                        ColumnTypeValues = ColumnTypeValues
                    };
                    vm.SetData(ui);

                    Columns.Add(vm);

                    SendMessage(new ColumnChangedMessage(vm, ColumnChangeKind.Added));
                }
            }

            SelectedColumn = Columns.FirstOrDefault();
        }

        #endregion

        #region Protected Overrides

        protected override void OnGetData(List<ReportColumnJson> data)
        {
            foreach (var columnVM in Columns) 
            {
                var ui = new ReportColumnJson();
                columnVM.GetData(ui);
                data.Add(ui);
            }
        }

        protected override void OnSetData(List<ReportColumnJson> data)
        {
            // map selected column to UI model
            Columns = data.Select(x => {
                var vm = new ColumnViewModel()
                {
                    ColumnTypeValues = ColumnTypeValues
                };
                vm.SetData(x);
                return vm;
            }).ToObservable();
            SelectedColumn = Columns.FirstOrDefault();
        }

        #endregion

        #region Private Methods

        private void AddColumn()
        {
            var ui = new ReportColumnJson
            {
                Key = "new_column",
                Type = ReportColumnType.String,
            };

            var vm = new ColumnViewModel()
            {
                ColumnTypeValues = ColumnTypeValues
            };
            vm.SetData(ui);
            Columns.Add(vm);
            SendMessage(new ColumnChangedMessage(vm, ColumnChangeKind.Added));
            SelectedColumn = vm;
        }

        private void RemoveSelectedColumn()
        {
            var vm = SelectedColumn;
            if (vm == null) return;
            // remove underlying json column by key
            Columns.Remove(vm);
            SendMessage(new ColumnChangedMessage(vm, ColumnChangeKind.Deleted));
            SelectedColumn = Columns.FirstOrDefault();
            NotifyStatus("Column removed.");
        }

        private bool CanMoveUp()
        {
            if (SelectedColumn == null) return false;
            var index = Columns.IndexOf(SelectedColumn);
            return index > 0;
        }

        private void MoveUp()
        {
            if (SelectedColumn == null) return;
            var index = Columns.IndexOf(SelectedColumn);
            if (index > 0)
            {
                Columns.Move(index, index - 1);
                RaiseCanExec();
            }
        }

        private bool CanMoveDown()
        {
            if (SelectedColumn == null) return false;
            var index = Columns.IndexOf(SelectedColumn);
            return index >= 0 && index < Columns.Count - 1;
        }

        private void MoveDown()
        {
            if (SelectedColumn == null) return;
            var index = Columns.IndexOf(SelectedColumn);
            if (index >= 0 && index < Columns.Count - 1)
            {
                Columns.Move(index, index + 1);
                RaiseCanExec();
            }
        }

        private void OnSelectedColumnChanged(ColumnViewModel? model)
        {
            RaiseCanExec();
        }

        private void OnGetColumnsMessageReceived(GetColumnsMessage message)
        {
            foreach (var columnVM in Columns)
            {
                message.Columns.Add(columnVM);
            }
        }

        private void RaiseCanExec()
        {

            MoveUpCommand?.RaiseCanExecuteChanged();
            MoveDownCommand?.RaiseCanExecuteChanged();
        }

        #endregion
    }
}
