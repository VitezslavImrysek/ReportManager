using ReportManager.Shared.Dto;
using System.Collections.Generic;

namespace ReportManager.DefinitionModel.Models.ReportDefinition;

public sealed class ReportColumnJson
{
	public string Key { get; set; } = string.Empty;
	public List<string> CategoryPath { get; set; } = [];
    public ReportColumnType Type { get; set; } = ReportColumnType.String;
	public ReportColumnFlagsJson Flags { get; set; } = ReportColumnFlagsJson.None;
	public FilterConfigJson? Filter { get; set; }
	public SortConfigJson? Sort { get; set; }
}
