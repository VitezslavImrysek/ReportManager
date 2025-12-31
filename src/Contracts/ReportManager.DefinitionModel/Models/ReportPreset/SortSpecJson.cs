using Newtonsoft.Json;
using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class SortSpecJson
{
	public required string ColumnKey { get; set; }
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
    public SortDirection Direction { get; set; }
}
