using ReportAdmin.App.Messages;
using ReportAdmin.App.Models.Definition;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ColumnViewModel : DataEditorVM<ReportColumnJson>, IColumn
    {
        #region Properties

        public string Key { get; set => SetValue(ref field, value, OnKeyChanged); } = string.Empty;
        public ReportColumnType Type { get; set => SetValue(ref field, value); }

        // flags expanded
        public bool AlwaysSelect { get; set => SetValue(ref field, value); }
        public bool Hidden { get; set => SetValue(ref field, value, OnHiddenChanged); }
        public bool PrimaryKey { get; set => SetValue(ref field, value); }
        public bool Virtual { get; set => SetValue(ref field, value); }
        public bool Filterable { get; set => SetValue(ref field, value, OnFilterableChanged); }
        public bool Sortable { get; set => SetValue(ref field, value, OnSortableChanged); }

        public FilterConfigUi? Filter { get; set => SetValue(ref field, value); }
        public SortConfigUi? Sort { get; set => SetValue(ref field, value); }

        public ObservableCollection<ReportColumnType> ColumnTypeValues { get; set => SetValue(ref field, value); } = new();
        public bool HasLookup { get; set => SetValue(ref field, value, OnHasLookupChanged); }

        #endregion

        #region Override Methods

        protected override void OnSetData(ReportColumnJson data)
        {
            Key = data.Key;
            Type = data.Type;

            Filter = (FilterConfigUi)data.Filter;
            Sort = (SortConfigUi)data.Sort;

            AlwaysSelect = data.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect);
            Hidden = data.Flags.HasFlag(ReportColumnFlagsJson.Hidden);
            PrimaryKey = data.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey);
            Filterable = data.Flags.HasFlag(ReportColumnFlagsJson.Filterable);
            Sortable = data.Flags.HasFlag(ReportColumnFlagsJson.Sortable);
            Virtual = data.Flags.HasFlag(ReportColumnFlagsJson.Virtual);

            HasLookup = data.Filter?.Lookup != null;
        }

        protected override void OnGetData(ReportColumnJson data)
        {
            data.Key = Key;
            data.Type = Type;
            data.Flags = ReportColumnFlagsJson.None;
            if (AlwaysSelect) data.Flags |= ReportColumnFlagsJson.AlwaysSelect;
            if (Hidden) data.Flags |= ReportColumnFlagsJson.Hidden;
            if (PrimaryKey) data.Flags |= ReportColumnFlagsJson.PrimaryKey;
            if (Filterable) data.Flags |= ReportColumnFlagsJson.Filterable;
            if (Sortable) data.Flags |= ReportColumnFlagsJson.Sortable;
            if (Virtual) data.Flags |= ReportColumnFlagsJson.Virtual;

            data.Filter = (FilterConfigJson)Filter;
            data.Sort = (SortConfigJson)Sort;
        }

        #endregion

        #region Private Methods

        private void OnHiddenChanged(bool hidden)
        {
            if (IsInitialized)
            {
                SendMessage(new ColumnChangedMessage(this, new ColumnPropertyValue(ColumnProperty.Hidden, !hidden, hidden)));
            }
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

        #endregion
    }
}
