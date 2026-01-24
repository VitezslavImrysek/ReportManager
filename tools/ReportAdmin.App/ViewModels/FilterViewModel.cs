using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Preset;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ReportAdmin.App.ViewModels
{
    public class FilterViewModel : DataEditorVM<ObservableCollection<FilterSpecUi>>
    {
        #region Ctor

        public FilterViewModel()
        {
            AddFilterCommand = new RelayCommand(AddFilter);
            RemoveFilterCommand = new RelayCommand(RemoveFilter, () => SelectedFilter != null);

            RegisterMessage<ColumnChangedMessage>(OnColumnChanged);
        }

        #endregion

        #region Properties

        public ObservableCollection<FilterRuleVm> Filters { get; } = new();

        public ObservableCollection<IColumn> FilterableColumns { get; } = new();
        public ObservableCollection<FilterOperation> FilterOperationValues { get; } = new(Enum.GetValues(typeof(FilterOperation)).Cast<FilterOperation>());

        public FilterRuleVm? SelectedFilter { get; set => SetValue(ref field, value); }

        #endregion

        #region Commands

        public RelayCommand AddFilterCommand { get; }
        public RelayCommand RemoveFilterCommand { get; }

        #endregion

        #region Overrides

        protected override void OnGetData(ObservableCollection<FilterSpecUi> data)
        {
            foreach (var f in Filters)
            {
                if (f.Column == null) continue;
                var values = ParseValues(f.ValuesText);

                if (f.Operation is FilterOperation.IsNull or FilterOperation.NotNull)
                    values.Clear();

                if (f.Operation is FilterOperation.Between)
                {
                    if (values.Count < 2) continue;
                    values = values.Take(2).ToList();
                }

                if (RequiresValues(f.Operation) && values.Count == 0)
                    continue;

                data.Add(new FilterSpecUi
                {
                    ColumnKey = f.Column.Key,
                    Operation = f.Operation,
                    Values = values.ToObservable()
                });
            }
        }

        protected override void OnSetData(ObservableCollection<FilterSpecUi> data)
        {
            Filters.Clear();
            FilterableColumns.Clear();

            var msg = SendMessage<GetColumnsMessage>();

            foreach (var col in msg.Columns.Where(c => c.Filterable && c.Filter?.Hidden == false))
                FilterableColumns.Add(col);

            foreach (var f in data)
            {
                var column = FilterableColumns.FirstOrDefault(c => c.Key == f.ColumnKey);
                if (column == null) continue;

                var vm = new FilterRuleVm
                {
                    Column = column,
                    Operation = f.Operation,
                    ValuesText = string.Join(Environment.NewLine, f.Values)
                };
                vm.PropertyChanged += FilterVm_PropertyChanged;
                Filters.Add(vm);
            }
        }

        #endregion

        #region Private Methods

        private void AddFilter()
        {
            var first = FilterableColumns.FirstOrDefault();
            var vm = new FilterRuleVm { Column = first, Operation = FilterOperation.Eq, ValuesText = string.Empty };
            vm.PropertyChanged += FilterVm_PropertyChanged;
            Filters.Add(vm);
            SelectedFilter = vm;
            RaiseCanExec();
        }

        private void RemoveFilter()
        {
            if (SelectedFilter == null) return;
            SelectedFilter.PropertyChanged -= FilterVm_PropertyChanged;
            Filters.Remove(SelectedFilter);
            SelectedFilter = Filters.LastOrDefault();
            RaiseCanExec();
        }

        private void OnColumnChanged(ColumnChangedMessage message)
        {
            switch (message.ChangeKind)
            {
                case ColumnChangeKind.Added:
                    if (message.Column.Filterable) FilterableColumns.Add(message.Column);
                    break;
                case ColumnChangeKind.Deleted:
                    if (message.Column.Filterable)
                    {
                        foreach (var filter in Filters.Where(x => x.Column == message.Column).ToList())
                        {
                            Filters.Remove(filter);
                        }
                        FilterableColumns.Remove(message.Column);
                    }
                    break;
                case ColumnChangeKind.Changed:
                    var pv = message.PropertyValue;
                    if (pv?.Property == ColumnProperty.Filterable)
                    {
                        if (message.Column.Filterable)
                        {
                            FilterableColumns.Add(message.Column);
                        }
                        else
                        {
                            FilterableColumns.Remove(message.Column);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void FilterVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FilterRuleVm.Operation))
            {
                if (sender is FilterRuleVm vm && (vm.Operation is FilterOperation.IsNull or FilterOperation.NotNull))
                    vm.ValuesText = string.Empty;
            }
        }

        private void RaiseCanExec()
        {
            AddFilterCommand.RaiseCanExecuteChanged();
            RemoveFilterCommand.RaiseCanExecuteChanged();
        }

        private static bool RequiresValues(FilterOperation op) =>
            op is not (FilterOperation.IsNull or FilterOperation.NotNull);

        private static List<string> ParseValues(string? text)
            => (text ?? "")
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .Select(x => (x ?? "").Trim())
                .Where(x => x.Length > 0)
                .ToList();

        #endregion
    }
}
