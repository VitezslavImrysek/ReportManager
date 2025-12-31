namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class QuerySpecJson
{
    public List<FilterSpecJson> Filters { get; set; } = new();
    public List<SortSpecJson> Sorting { get; set; } = new();
    public List<string> SelectedColumns { get; set; } = new();
}
