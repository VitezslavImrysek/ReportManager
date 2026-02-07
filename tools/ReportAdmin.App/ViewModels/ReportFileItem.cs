using ReportAdmin.Core;
using ReportManager.Lib.Wpf;

namespace ReportAdmin.App.ViewModels;

public sealed class ReportFileItem : NotificationObject
{
	public string FilePath { get; set; } = string.Empty;
	public string FileName => System.IO.Path.GetFileName(FilePath);
	public override string ToString() => FileName;
}
