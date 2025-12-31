using ReportManager.DefinitionModel.Models.ReportDefinition;

namespace ReportAdmin.Core.Models.Definition;

public sealed class SortConfigUi : NotificationObject
{
    public bool Enabled { get; set => SetValue(ref field, value); }
    public bool Hidden { get; set => SetValue(ref field, value); }

    public static explicit operator SortConfigJson(SortConfigUi ui)
    {
        if (ui == null) return null!;
        var flags = SortConfigFlagsJson.None;
        if (ui.Hidden) flags |= SortConfigFlagsJson.Hidden;
        return new SortConfigJson { Enabled = ui.Enabled, Flags = flags };
    }

    public static explicit operator SortConfigUi(SortConfigJson src)
    {
        if (src == null) return null!;
        var ui = new SortConfigUi
        {
            Enabled = src.Enabled,
            Hidden = src.Flags.HasFlag(SortConfigFlagsJson.Hidden)
        };
        return ui;
    }
}
