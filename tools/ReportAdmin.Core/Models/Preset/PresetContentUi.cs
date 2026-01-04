using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models.Preset;

public sealed class PresetContentUi : NotificationObject
{
    public GridStateUi Grid { get; set => SetValue(ref field, value); } = new();
    public QuerySpecUi Query { get; set => SetValue(ref field, value); } = new();
    public Dictionary<string, Dictionary<string, string>> Texts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public static explicit operator PresetContentJson(PresetContentUi ui)
    {
        if (ui == null) return null!;
        return new PresetContentJson { Grid = (GridStateJson)ui.Grid, Query = (QuerySpecJson)ui.Query, Texts = ui.Texts };
    }

    public static explicit operator PresetContentUi(PresetContentJson src)
    {
        if (src == null) return null!;
        return new PresetContentUi { Grid = (GridStateUi)src.Grid, Query = (QuerySpecUi)src.Query, Texts = src.Texts };
    }
}
