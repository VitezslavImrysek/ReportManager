using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models.Preset;

public sealed class SystemPresetUi : NotificationObject
{
    public string PresetKey { get; set => SetValue(ref field, value); } = string.Empty;
    public Guid PresetId { get; set => SetValue(ref field, value); }
    public string Name { get; set => SetValue(ref field, value); } = string.Empty;
    public bool IsDefault { get; set => SetValue(ref field, value); }
    public PresetContentUi Content { get; set => SetValue(ref field, value); } = new PresetContentUi();

    public static explicit operator SystemPreset(SystemPresetUi ui)
    {
        if (ui == null) return null!;
        return new SystemPreset { Content = (PresetContentJson)ui.Content, IsDefault = ui.IsDefault, Name = ui.Name, PresetId = ui.PresetId, PresetKey = ui.PresetKey };
    }

    public static explicit operator SystemPresetUi(SystemPreset src)
    {
        if (src == null) return null!;
        return new SystemPresetUi { Content = (PresetContentUi)src.Content, IsDefault = src.IsDefault, Name = src.Name, PresetId = src.PresetId, PresetKey = src.PresetKey };
    }
}
