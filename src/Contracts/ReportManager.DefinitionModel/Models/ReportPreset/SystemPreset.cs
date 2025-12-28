namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class SystemPreset
{
	public string PresetKey { get; set; } = "";
	public Guid PresetId { get; set; }
	public string Name { get; set; } = "";
	public bool IsDefault { get; set; }
	public PresetContentJson Content { get; set; } = new();
}
