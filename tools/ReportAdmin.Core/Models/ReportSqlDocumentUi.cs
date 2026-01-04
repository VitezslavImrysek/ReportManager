using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Models.Preset;
using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models;

public sealed class ReportSqlDocumentUi : NotificationObject
{
	public string? FilePath { get; set => SetValue(ref field, value); }
    public required string ReportKey { get; set => SetValue(ref field, value); }
    public required string ViewSchema { get; set => SetValue(ref field, value); }
    public required string ViewName { get; set => SetValue(ref field, value); }

	public ReportDefinitionUi? Definition { get; set; }
	public List<SystemPresetUi> SystemPresets { get; set; } = [];
}
