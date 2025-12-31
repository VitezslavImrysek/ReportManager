using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models.Preset;

public sealed class GridStateUi : NotificationObject
{
    public ObservableCollection<string> HiddenColumns { get; set => SetValue(ref field, value); } = new();
    public ObservableCollection<string> Order { get; set => SetValue(ref field, value); } = new();

    public static explicit operator GridStateJson(GridStateUi ui)
    {
        if (ui == null) return null!;
        return new GridStateJson { HiddenColumns = ui.HiddenColumns.ToList(), Order = ui.Order.ToList() };
    }

    public static explicit operator GridStateUi(GridStateJson src)
    {
        if (src == null) return null!;
        var ui = new GridStateUi();
        ui.HiddenColumns.Clear();
        if (src.HiddenColumns != null) foreach (var s in src.HiddenColumns) ui.HiddenColumns.Add(s);
        ui.Order.Clear();
        if (src.Order != null) foreach (var s in src.Order) ui.Order.Add(s);
        return ui;
    }
}
