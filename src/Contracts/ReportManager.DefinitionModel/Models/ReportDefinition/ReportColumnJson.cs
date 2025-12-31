using Newtonsoft.Json;
using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class ReportColumnJson
{
	public required string Key { get; set; }
	public required string TextKey { get; set; }
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public ReportColumnType Type { get; set; } = ReportColumnType.String;
	public ReportColumnFlagsJson Flags { get; set; } = ReportColumnFlagsJson.None;
	public FilterConfigJson Filter { get; set; } = new();
	public SortConfigJson Sort { get; set; } = new();
}
