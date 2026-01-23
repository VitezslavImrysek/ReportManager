using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models;

public sealed class SortSpecJson
{
    public required string ColumnKey { get; set; }
    public SortDirection Direction { get; set; }


    public static explicit operator SortSpecJson(SortSpecDto dto)
    {
        if (dto == null) return null!;
        return new SortSpecJson
        {
            ColumnKey = dto.ColumnKey,
            Direction = dto.Direction
        };
    }

    public static explicit operator SortSpecDto(SortSpecJson src)
    {
        if (src == null) return null!;
        return new SortSpecDto
        {
            ColumnKey = src.ColumnKey,
            Direction = src.Direction
        };
    }
}
