namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class SqlLookupJson
{
	public string CommandText { get; set; } = "";
	public string KeyColumn { get; set; } = "Id";
	public string TextColumn { get; set; } = "Name";
}
