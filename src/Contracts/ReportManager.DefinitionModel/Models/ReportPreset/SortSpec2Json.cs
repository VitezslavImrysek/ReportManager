using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class SortSpec2Json
{
	public string ColumnKey { get; set; } = "";
	public SortDirection Direction { get; set; } = SortDirection.Asc;
}
