using ReportManager.ApiContracts;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class ReportDefinitionJson
{
	public int Version { get; set; } = 1;
	public string DefaultCulture { get; set; } = Constants.DefaultLanguage;
	public string? TextKey { get; set; }
	public Dictionary<string, Dictionary<string, string>> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
	public List<ReportColumnJson> Columns { get; set; } = new();
	public List<SortSpecJson> DefaultSort { get; set; } = new();
}
