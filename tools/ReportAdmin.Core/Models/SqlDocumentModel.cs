using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models;

public sealed class ReportSqlDocument
{
	public string? FilePath { get; set; }
	public required string ReportKey { get; set; }
	public required string ReportName { get; set; }
	public required string ViewSchema { get; set; }
	public required string ViewName { get; set; }
	public int Version { get; set; } = 1;

	public ReportDefinitionJson? Definition { get; set; }
	public List<SystemPreset> SystemPresets { get; set; } = [];
}
