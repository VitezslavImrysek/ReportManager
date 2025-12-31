using ReportAdmin.Core;

namespace ReportAdmin.App.ViewModels;

public sealed class ReportFileItem : NotificationObject
{
	public required string FilePath { get; init; }
	public string FileName => System.IO.Path.GetFileName(FilePath);
	public override string ToString() => FileName;
}
