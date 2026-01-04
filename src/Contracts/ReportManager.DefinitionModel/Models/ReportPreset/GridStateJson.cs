using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class GridStateJson
{
    public List<string> HiddenColumns { get; set; } = new();
    public List<string> Order { get; set; } = new();

    public static explicit operator GridStateJson(GridStateDto dto)
    {
        if (dto == null) return null!;
        return new GridStateJson { HiddenColumns = dto.HiddenColumns, Order = dto.Order };
    }

    public static explicit operator GridStateDto(GridStateJson src)
    {
        if (src == null) return null!;
        return new GridStateDto { HiddenColumns = src.HiddenColumns, Order = src.Order };
    }
}
