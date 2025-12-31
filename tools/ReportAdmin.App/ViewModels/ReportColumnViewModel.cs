using ReportAdmin.Core;
using ReportAdmin.Core.Models.Definition;

namespace ReportAdmin.App.ViewModels
{
    public class ReportColumnViewModel : NotificationObject
    {
        public ReportColumnUi? Column { get; set => SetValue(ref field, value); }
    }
}
