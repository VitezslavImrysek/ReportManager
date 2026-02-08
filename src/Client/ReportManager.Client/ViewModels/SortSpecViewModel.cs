using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
    public sealed class SortSpecViewModel : NotificationObject
    {
        public ObservableCollection<ColumnPickerItem> AvailableColumnItems { get; set; } = [];
        public ObservableCollection<SortDirection> Directions { get; } = [SortDirection.Asc, SortDirection.Desc];
        public ColumnPickerItem? SelectedColumnItem { get; set => SetValue(ref field, value, OnSelectedColumnItemChanged); }
        public ColumnOption? SelectedColumn { get; set => SetValue(ref field, value); }
        public SortDirection SelectedDirection { get; set => SetValue(ref field, value); }

        public ICommand? RemoveCommand { get; set; }

        public void SelectColumn(ColumnOption? column)
        {
            if (column == null)
            {
                SelectedColumnItem = null;
                return;
            }

            SelectedColumnItem = AvailableColumnItems.FirstOrDefault(item =>
                item.Column != null
                && item.Column.Key.Equals(column.Key, StringComparison.OrdinalIgnoreCase));
        }

        private void OnSelectedColumnItemChanged(ColumnPickerItem? pickerItem)
        {
            if (pickerItem?.Column == null)
            {
                return;
            }

            SelectedColumn = pickerItem.Column;
        }
    }
}
