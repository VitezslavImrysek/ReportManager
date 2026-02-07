using ReportAdmin.Core;
using ReportManager.Lib.Wpf;

namespace ReportAdmin.App.ViewModels;

public sealed class ColumnVisibilityViewModel : NotificationObject
{
	public required IColumn Column { get; set => SetValue(ref field, value); }
	public bool IsVisible { get; set => SetValue(ref field, value); }
}
