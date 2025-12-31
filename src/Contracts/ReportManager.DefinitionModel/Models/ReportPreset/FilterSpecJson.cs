using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class FilterSpecJson
{
	public required string ColumnKey { get; set; }
	public FilterOperation Operation { get; set; }
    public List<string> Values { get; set; } = new();
}
