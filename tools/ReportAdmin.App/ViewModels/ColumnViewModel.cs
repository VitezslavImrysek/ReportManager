using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Definition;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ColumnViewModel : DataEditorVM<ReportColumnUi, object>, IColumn
    {
        public string Key { get; set => SetValue(ref field, value, OnKeyChanged); } = string.Empty;
        public ReportColumnType Type { get; set => SetValue(ref field, value); }

        // flags expanded
        public bool AlwaysSelect { get; set => SetValue(ref field, value); }
        public bool Hidden { get; set => SetValue(ref field, value); }
        public bool PrimaryKey { get; set => SetValue(ref field, value); }
        public bool Virtual { get; set => SetValue(ref field, value); }
        public bool Filterable { get; set => SetValue(ref field, value, OnFilterableChanged); }
        public bool Sortable { get; set => SetValue(ref field, value, OnSortableChanged); }

        public FilterConfigUi? Filter { get; set => SetValue(ref field, value); }
        public SortConfigUi? Sort { get; set => SetValue(ref field, value); }

        public ObservableCollection<ReportColumnType> ColumnTypeValues { get; set => SetValue(ref field, value); } = new();
        public bool HasLookup { get; set => SetValue(ref field, value, OnHasLookupChanged); }

        protected override void OnNew(object context)
        {

        }

        protected override void OnSetData(ReportColumnUi data)
        {
            Key = data.Key;
            Type = data.Type;

            Filter = data.Filter;
            Sort = data.Sort;

            AlwaysSelect = data.AlwaysSelect;
            Hidden = data.Hidden;
            PrimaryKey = data.PrimaryKey;
            Filterable = data.Filterable;
            Sortable = data.Sortable;
            Virtual = data.Virtual;

            HasLookup = data.Filter?.Lookup != null;
        }

        protected override void OnGetData(ReportColumnUi data)
        {
            data.Key = Key;
            data.Type = Type;
            data.AlwaysSelect = AlwaysSelect;
            data.Hidden = Hidden;
            data.PrimaryKey = PrimaryKey;
            data.Filterable = Filterable;
            data.Sortable = Sortable;
            data.Virtual = Virtual;
            data.Filter = Filter;
            data.Sort = Sort;
        }

        private void OnFilterableChanged(bool filterable)
        {
            if (IsInitialized)
            {
                Filter = filterable ? (Filter ?? new FilterConfigUi()) : null;
                SendMessage(new ColumnChangedMessage(this, new ColumnPropertyValue(ColumnProperty.Filterable, !filterable, filterable)));
            }
        }

        private void OnSortableChanged(bool sortable)
        {
            if (IsInitialized)
            {
                Sort = sortable ? (Sort ?? new SortConfigUi()) : null;
                SendMessage(new ColumnChangedMessage(this, new ColumnPropertyValue(ColumnProperty.Sortable, !sortable, sortable)));
            }
        }

        private void OnHasLookupChanged(bool hasLookup)
        {
            if (IsInitialized && Filter != null)
            {
                if (hasLookup)
                {
                    Filter.Lookup = Filter.Lookup ?? new LookupConfigUi();
                    Filter.Lookup.Mode = LookupMode.Sql;
                    Filter.Lookup.Sql = new SqlLookupUi();
                }
                else
                {
                    Filter.Lookup = null;
                }
            }
        }

        private void OnKeyChanged(string oldKey, string newKey)
        {
            if (IsInitialized)
            {
                // notify key changed
                SendMessage(new ColumnChangedMessage(this, new ColumnPropertyValue(ColumnProperty.Key, oldKey, newKey)));
            }
        }
    }
}
