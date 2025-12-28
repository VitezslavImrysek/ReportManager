using ReportManager.ApiContracts.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class FilterSpecJson
{
	public string ColumnKey { get; set; } = "";
	public FilterOperation Operation { get; set; } = FilterOperation.Eq;
	public List<string> Values { get; set; } = new();
}
