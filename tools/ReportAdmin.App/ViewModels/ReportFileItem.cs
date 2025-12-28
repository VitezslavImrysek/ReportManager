namespace ReportAdmin.App.ViewModels;

public sealed class ReportFileItem
{
	public required string FilePath { get; init; }
	public string FileName => System.IO.Path.GetFileName(FilePath);
	public override string ToString() => FileName;
}
