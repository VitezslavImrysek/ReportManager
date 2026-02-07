using ReportAdmin.Core;
using ReportManager.Lib.Wpf;

namespace ReportAdmin.App.ViewModels;

public sealed class TextEntryViewModel : NotificationObject
{
	public string? Key { get; set => SetValue(ref field, value); }
	public string? Value { get; set => SetValue(ref field, value); }
}
