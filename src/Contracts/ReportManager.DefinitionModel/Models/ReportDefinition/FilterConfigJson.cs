namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class FilterConfigJson 
{ 
	public LookupConfigJson? Lookup { get; set; }
	public FilterConfigFlagsJson Flags { get; set; }
}
