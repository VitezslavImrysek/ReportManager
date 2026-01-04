using ReportManager.Shared.Dto;

namespace ReportManager.DefinitionModel.Models.ReportPreset;

public sealed class FilterSpecJson
{
	public required string ColumnKey { get; set; }
	public FilterOperation Operation { get; set; }
    public List<string> Values { get; set; } = new();

	public static explicit operator FilterSpecJson(FilterSpecDto dto)
	{
		if (dto == null) return null!;
		return new FilterSpecJson
		{
			ColumnKey = dto.ColumnKey,
			Operation = dto.Operation,
			Values = dto.Values
		};
	}

	public static explicit operator FilterSpecDto(FilterSpecJson src)
	{
		if (src == null) return null!;
		return new FilterSpecDto
		{
			ColumnKey = src.ColumnKey,
			Operation = src.Operation,
			Values = src.Values
		};
    }
}
