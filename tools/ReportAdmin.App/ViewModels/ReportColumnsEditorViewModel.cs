using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
using ReportAdmin.Core.Db;
using ReportAdmin.Core.Models.Definition;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ReportColumnsEditorViewModel : DataEditorVM<ObservableCollection<ReportColumnUi>, object>
    {
        public ReportColumnsEditorViewModel()
        {
            Columns = [];

            AddColumnCommand = new RelayCommand(AddColumn);
            RemoveSelectedColumnCommand = new RelayCommand(RemoveSelectedColumn);

            Messenger.Instance.Register<GetColumnsMessage>(OnGetColumnsMessageReceived);
        }

        #region Properties

        public ObservableCollection<ReportColumnType> ColumnTypeValues { get; } = new(Enum.GetValues(typeof(ReportColumnType)).Cast<ReportColumnType>());
        public ObservableCollection<ReportColumnViewModel> Columns { get; set => SetValue(ref field, value); }
        public ReportColumnViewModel? SelectedColumn { get; set => SetValue(ref field, value); }

        #endregion

        #region Commands

        public RelayCommand AddColumnCommand { get; }
        public RelayCommand RemoveSelectedColumnCommand { get; }

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
                    Messenger.Instance.Send(new ReportColumnKeyChangedMessage() { OldName = col.Key });
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

                    var ui = new ReportColumnUi
                    {
                        Key = col.Name,
                        Type = type,
                    };

                    var vm = new ReportColumnViewModel()
                    {
                        ColumnTypeValues = ColumnTypeValues
                    };
                    vm.SetData(ui);

                    Columns.Add(vm);

                    Messenger.Instance.Send(new ReportColumnKeyChangedMessage() { NewName = col.Name });
                }
            }

            SelectedColumn = Columns.FirstOrDefault();
        }

        #endregion

        #region Protected Overrides

        protected override void OnGetData(ObservableCollection<ReportColumnUi> data)
        {
            foreach (var columnVM in Columns) 
            {
                var ui = new ReportColumnUi();
                columnVM.GetData(ui);
                data.Add(ui);
            }
        }

        protected override void OnSetData(ObservableCollection<ReportColumnUi> data)
        {
            // map selected column to UI model
            Columns = data.Select(x => {
                var vm = new ReportColumnViewModel()
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
            var ui = new ReportColumnUi
            {
                Key = "new_column",
                Type = ReportColumnType.String,
            };

            var vm = new ReportColumnViewModel()
            {
                ColumnTypeValues = ColumnTypeValues
            };
            vm.SetData(ui);
            Columns.Add(vm);
            SelectedColumn = vm;
        }

        private void RemoveSelectedColumn()
        {
            if (SelectedColumn == null) return;
            // remove underlying json column by key
            Columns.Remove(SelectedColumn);
            SelectedColumn = Columns.FirstOrDefault();
            NotifyStatus("Column removed.");
        }

        private void OnGetColumnsMessageReceived(GetColumnsMessage message)
        {
            foreach (var columnVM in Columns)
            {
                var column = new ReportColumnUi();
                columnVM.GetData(column);

                message.Columns.Add(column);
            }
        }

        #endregion
    }
}
