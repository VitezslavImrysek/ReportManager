namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class GridStateJson
{
	public List<string> HiddenColumns { get; set; } = new();
	public List<string> Order { get; set; } = new();
}
