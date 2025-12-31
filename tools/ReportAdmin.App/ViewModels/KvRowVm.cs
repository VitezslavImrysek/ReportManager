using ReportAdmin.Core;

namespace ReportAdmin.App.ViewModels;

public sealed class KvRowVm : NotificationObject
{
	public required string Key { get; set => SetValue(ref field, value); }
	public required string Value { get; set => SetValue(ref field, value); }
}
