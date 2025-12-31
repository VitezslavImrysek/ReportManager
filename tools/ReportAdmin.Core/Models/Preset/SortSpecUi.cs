using ReportManager.DefinitionModel.Models.ReportPreset;
using ReportManager.Shared.Dto;

namespace ReportAdmin.Core.Models.Preset;

public sealed class SortSpecUi : NotificationObject
{
    public string ColumnKey { get; set => SetValue(ref field, value); } = string.Empty;
    public SortDirection Direction { get; set => SetValue(ref field, value); }

    public static explicit operator SortSpecJson(SortSpecUi ui)
    {
        if (ui == null) return null!;
        return new SortSpecJson { ColumnKey = ui.ColumnKey, Direction = ui.Direction };
    }

    public static explicit operator SortSpecUi(SortSpecJson src)
    {
        if (src == null) return null!;
        return new SortSpecUi { ColumnKey = src.ColumnKey, Direction = src.Direction };
    }
}
