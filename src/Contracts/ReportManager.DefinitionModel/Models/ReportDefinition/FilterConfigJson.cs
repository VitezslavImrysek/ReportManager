namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class FilterConfigJson 
{ 
	public bool Enabled { get; set; } = true;
	public LookupConfigJson? Lookup { get; set; }
}
