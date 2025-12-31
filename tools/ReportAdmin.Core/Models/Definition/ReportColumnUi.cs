using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared.Dto;

namespace ReportAdmin.Core.Models.Definition;

public sealed class ReportColumnUi : NotificationObject
{
    public required string Key { get; set => SetValue(ref field, value); }
    public required string TextKey { get; set => SetValue(ref field, value); }
    public ReportColumnType Type { get; set => SetValue(ref field, value); }

    // flags expanded
    public bool AlwaysSelect { get; set => SetValue(ref field, value); }
    public bool Hidden { get; set => SetValue(ref field, value); }
    public bool PrimaryKey { get; set => SetValue(ref field, value); }

    public required FilterConfigUi Filter { get; set => SetValue(ref field, value); }
    public required SortConfigUi Sort { get; set => SetValue(ref field, value); }

    public static explicit operator ReportColumnJson(ReportColumnUi ui)
    {
        if (ui == null) return null!;
        var r = new ReportColumnJson
        {
            Key = ui.Key,
            TextKey = ui.TextKey,
            Type = ui.Type,
            Flags = ReportColumnFlagsJson.None,
            Filter = (FilterConfigJson)ui.Filter,
            Sort = (SortConfigJson)ui.Sort
        };
        if (ui.AlwaysSelect) r.Flags |= ReportColumnFlagsJson.AlwaysSelect;
        if (ui.Hidden) r.Flags |= ReportColumnFlagsJson.Hidden;
        if (ui.PrimaryKey) r.Flags |= ReportColumnFlagsJson.PrimaryKey;
        return r;
    }

    public static explicit operator ReportColumnUi(ReportColumnJson src)
    {
        if (src == null) return null!;
        var ui = new ReportColumnUi
        {
            Key = src.Key,
            TextKey = src.TextKey,
            Type = src.Type,
            Filter = (FilterConfigUi)(src.Filter ?? new FilterConfigJson()),
            Sort = (SortConfigUi)(src.Sort ?? new SortConfigJson()),
            AlwaysSelect = src.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect),
            Hidden = src.Flags.HasFlag(ReportColumnFlagsJson.Hidden),
            PrimaryKey = src.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey)
        };
        return ui;
    }
}
