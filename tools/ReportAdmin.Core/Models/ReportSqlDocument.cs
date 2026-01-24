using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models;

public sealed class ReportSqlDocument
{
	public string? FilePath { get; set; }
    public string ReportKey { get; set; } = string.Empty;
    public string ViewSchema { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;

    public ReportDefinitionJson Definition { get; set; } = new ReportDefinitionJson();
	public List<SystemPreset> SystemPresets { get; set; } = [];
}
