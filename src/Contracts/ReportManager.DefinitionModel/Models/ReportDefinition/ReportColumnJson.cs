using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class ReportColumnJson
{
	public required string Key { get; set; }
	public required string TextKey { get; set; }
	public ReportColumnType Type { get; set; } = ReportColumnType.String;

	public bool Hidden { get; set; }
	public bool AlwaysSelect { get; set; }

	public FilterConfigJson Filter { get; set; } = new();
	public SortConfigJson Sort { get; set; } = new();
}
