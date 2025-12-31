namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class PresetContentJson
{
    public int Version { get; set; } = 1;
    public GridStateJson Grid { get; set; } = new();
    public QuerySpecJson Query { get; set; } = new();
}
