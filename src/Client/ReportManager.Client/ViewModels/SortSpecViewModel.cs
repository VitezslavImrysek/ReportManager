using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
    public sealed class SortSpecViewModel : NotificationObject
    {
        public ObservableCollection<ColumnOption> AvailableColumns { get; set; } = [];
        public ObservableCollection<SortDirection> Directions { get; } = [SortDirection.Asc, SortDirection.Desc];
        public ColumnOption? SelectedColumn { get; set => SetValue(ref field, value); }
        public SortDirection SelectedDirection { get; set => SetValue(ref field, value); }

        public ICommand? RemoveCommand { get; set; }
    }
}
