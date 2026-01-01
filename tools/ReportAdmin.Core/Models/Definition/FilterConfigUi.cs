using ReportManager.DefinitionModel.Models.ReportDefinition;

namespace ReportAdmin.Core.Models.Definition;

public sealed class FilterConfigUi : NotificationObject
{
    public LookupConfigUi? Lookup { get; set => SetValue(ref field, value); }
    public bool Hidden { get; set => SetValue(ref field, value); }

    public static explicit operator FilterConfigJson(FilterConfigUi ui)
    {
        if (ui == null) return null!;
        var flags = FilterConfigFlagsJson.None;
        if (ui.Hidden) flags |= FilterConfigFlagsJson.Hidden;
        return new FilterConfigJson 
        { 
            Lookup = ui.Lookup == null ? null : (LookupConfigJson)ui.Lookup, 
            Flags = flags 
        };
    }

    public static explicit operator FilterConfigUi(FilterConfigJson src)
    {
        if (src == null) return null!;
        return new FilterConfigUi
        {
            Lookup = src.Lookup == null ? null : (LookupConfigUi)src.Lookup,
            Hidden = src.Flags.HasFlag(FilterConfigFlagsJson.Hidden)
        };
    }
}
