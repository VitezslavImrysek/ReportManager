using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class SortSpecJson
{
	public required string Column { get; set; }
    public SortDirection Dir { get; set; } = SortDirection.Asc;
}
