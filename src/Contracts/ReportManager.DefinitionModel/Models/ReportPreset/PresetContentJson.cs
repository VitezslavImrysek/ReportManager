using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class PresetContentJson
{
    public GridStateJson Grid { get; set; } = new();
    public QuerySpecJson Query { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static explicit operator PresetContentJson(PresetContentDto dto)
    {
        if (dto == null) return null!;
        return new PresetContentJson { Grid = (GridStateJson)dto.Grid, Query = (QuerySpecJson)dto.Query };
    }

    public static explicit operator PresetContentDto(PresetContentJson src)
    {
        if (src == null) return null!;
        return new PresetContentDto { Grid = (GridStateDto)src.Grid, Query = (QuerySpecDto)src.Query };
    }
}
