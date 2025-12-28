using ReportManager.ApiContracts.Dto;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class LookupConfigJson
{
	public LookupMode Mode { get; set; } = LookupMode.Sql;
	public SqlLookupJson? Sql { get; set; } = new();
	public List<LookupItemJson>? Items { get; set; }
}
