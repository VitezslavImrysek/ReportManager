using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models;

public sealed class ReportSqlDocumentUi : NotificationObject
{
	public string? FilePath { get; set => SetValue(ref field, value); }
    public string ReportKey { get; set => SetValue(ref field, value); } = string.Empty;
    public string ViewSchema { get; set => SetValue(ref field, value); } = string.Empty;
    public string ViewName { get; set => SetValue(ref field, value); } = string.Empty;

    public ReportDefinitionJson Definition { get; set; } = new ReportDefinitionJson();
	public List<SystemPreset> SystemPresets { get; set; } = [];
}
