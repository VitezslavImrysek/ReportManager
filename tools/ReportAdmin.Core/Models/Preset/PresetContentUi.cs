using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.Core.Models.Preset;

public sealed class PresetContentUi : NotificationObject
{
    public int Version { get; set => SetValue(ref field, value); } = 1;
    public GridStateUi Grid { get; set => SetValue(ref field, value); } = new();
    public QuerySpecUi Query { get; set => SetValue(ref field, value); } = new();

    public static explicit operator PresetContentJson(PresetContentUi ui)
    {
        if (ui == null) return null!;
        return new PresetContentJson { Version = ui.Version, Grid = (GridStateJson)ui.Grid, Query = (QuerySpecJson)ui.Query };
    }

    public static explicit operator PresetContentUi(PresetContentJson src)
    {
        if (src == null) return null!;
        return new PresetContentUi { Version = src.Version, Grid = (GridStateUi)src.Grid, Query = (QuerySpecUi)src.Query };
    }
}
