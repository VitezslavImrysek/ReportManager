using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class QuerySpecJson
{
    public List<FilterSpecJson> Filters { get; set; } = new();
    public List<SortSpecJson> Sorting { get; set; } = new();
    public List<string> SelectedColumns { get; set; } = new();

    public static explicit operator QuerySpecJson(QuerySpecDto dto)
    {
        if (dto == null) return null!;
        return new QuerySpecJson
        {
            Filters = dto.Filters.Select(f => (FilterSpecJson)f).ToList(),
            Sorting = dto.Sorting.Select(s => (SortSpecJson)s).ToList(),
            SelectedColumns = dto.SelectedColumns
        };
    }

    public static explicit operator QuerySpecDto(QuerySpecJson src)
    {
        if (src == null) return null!;
        return new QuerySpecDto
        {
            Filters = src.Filters.Select(f => (FilterSpecDto)f).ToList(),
            Sorting = src.Sorting.Select(s => (SortSpecDto)s).ToList(),
            SelectedColumns = src.SelectedColumns
        };
    }
}
