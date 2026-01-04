using ReportAdmin.Core;

namespace ReportAdmin.App.ViewModels;

public sealed class ColumnVisibilityRowVm : NotificationObject
{
	public required string Key { get; set => SetValue(ref field, value); }
    public required string Caption { get; set => SetValue(ref field, value); }
	public bool IsVisible { get; set => SetValue(ref field, value); }
}
