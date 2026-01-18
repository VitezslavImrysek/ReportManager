using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Models.Preset;
using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models;

public sealed class ReportSqlDocumentUi : NotificationObject
{
	public string? FilePath { get; set => SetValue(ref field, value); }
    public string ReportKey { get; set => SetValue(ref field, value); }
    public string ViewSchema { get; set => SetValue(ref field, value); }
    public string ViewName { get; set => SetValue(ref field, value); }

	public ReportDefinitionUi? Definition { get; set; }
	public ObservableCollection<SystemPresetUi> SystemPresets { get; set; } = [];
}
