using ReportAdmin.App.ViewModels;

namespace ReportAdmin.App.Messages
{
    public class ColumnChangedMessage
    {
        public ColumnChangedMessage(IColumn column, ColumnChangeKind changeKind)
        {
            if (changeKind == ColumnChangeKind.Changed)
            {
                throw new ArgumentException(nameof(changeKind));
            }

            Column = column;
            ChangeKind = changeKind;
        }

        public ColumnChangedMessage(IColumn column, ColumnPropertyValue propertyValue)
        {
            Column = column;
            ChangeKind = ColumnChangeKind.Changed;
            PropertyValue = propertyValue;
        }

        public IColumn Column { get; }
        public ColumnChangeKind ChangeKind { get; }
        public ColumnPropertyValue? PropertyValue { get; }
    }

    public class ColumnPropertyValue
    {
        public ColumnPropertyValue(ColumnProperty property, object? oldValue, object? newValue)
        {
            Property = property;
            OldValue = oldValue;
            NewValue = newValue;
        }

        public ColumnProperty Property { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }
    }

    public enum ColumnChangeKind 
    {
        Added,
        Deleted,
        Changed
    }

    public enum ColumnProperty
    { 
        Key,
        CategoryPath,
        Hidden,
        Sortable,
        Filterable
    }
}
