using ReportAdmin.Core;
using ReportAdmin.Core.Models.Definition;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class ReportColumnViewModel : NotificationObject
    {
        public ReportColumnUi? Column
        {
            get => field;
            set
            {
                if (SetValue(ref field, value))
                {
                    OnPropertyChanged(nameof(HasColumn));
                }
            }
        }

        public ObservableCollection<ReportColumnType> ColumnTypeValues { get; set => SetValue(ref field, value); } = new();

        public bool HasColumn => Column != null;
    }
}
