using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared.Dto;

namespace ReportAdmin.Core.Models.Definition;

public sealed class ReportColumnUi : NotificationObject
{
    public required string Key { get; set => SetValue(ref field, value); }
    public ReportColumnType Type { get; set => SetValue(ref field, value); }

    // flags expanded
    public bool AlwaysSelect { get; set => SetValue(ref field, value); }
    public bool Hidden { get; set => SetValue(ref field, value); }
    public bool PrimaryKey { get; set => SetValue(ref field, value); }
    public bool Virtual { get; set => SetValue(ref field, value); }
    public bool Filterable { get; set => SetValue(ref field, value, OnFilterableChanged); }
    public bool Sortable { get; set => SetValue(ref field, value, OnSortableChanged); }

    public FilterConfigUi? Filter { get; set => SetValue(ref field, value); }
    public SortConfigUi? Sort { get; set => SetValue(ref field, value); }

    public static explicit operator ReportColumnJson(ReportColumnUi ui)
    {
        if (ui == null) return null!;
        var r = new ReportColumnJson
        {
            Key = ui.Key,
            Type = ui.Type,
            Flags = ReportColumnFlagsJson.None,
            Filter = ui.Filter == null ? null : (FilterConfigJson)ui.Filter,
            Sort = ui.Sort == null ? null : (SortConfigJson)ui.Sort
        };
        if (ui.AlwaysSelect) r.Flags |= ReportColumnFlagsJson.AlwaysSelect;
        if (ui.Hidden) r.Flags |= ReportColumnFlagsJson.Hidden;
        if (ui.PrimaryKey) r.Flags |= ReportColumnFlagsJson.PrimaryKey;
        if (ui.Filterable) r.Flags |= ReportColumnFlagsJson.Filterable;
        if (ui.Sortable) r.Flags |= ReportColumnFlagsJson.Sortable;
        if (ui.Virtual) r.Flags |= ReportColumnFlagsJson.Virtual;
        return r;
    }

    public static explicit operator ReportColumnUi(ReportColumnJson src)
    {
        if (src == null) return null!;
        var ui = new ReportColumnUi
        {
            Key = src.Key,
            Type = src.Type,
            Filter = src.Filter == null ? null : (FilterConfigUi)src.Filter,
            Sort = src.Sort == null ? null : (SortConfigUi)src.Sort,
            AlwaysSelect = src.Flags.HasFlag(ReportColumnFlagsJson.AlwaysSelect),
            Hidden = src.Flags.HasFlag(ReportColumnFlagsJson.Hidden),
            PrimaryKey = src.Flags.HasFlag(ReportColumnFlagsJson.PrimaryKey),
            Filterable = src.Flags.HasFlag(ReportColumnFlagsJson.Filterable),
            Sortable = src.Flags.HasFlag(ReportColumnFlagsJson.Sortable),
            Virtual = src.Flags.HasFlag(ReportColumnFlagsJson.Virtual)
        };
        return ui;
    }

    private void OnFilterableChanged(bool filterable)
    {
        Filter = filterable ? (Filter ?? new FilterConfigUi()) : null;
    }

    private void OnSortableChanged(bool sortable)
    {
        Sort = sortable ? (Sort ?? new SortConfigUi()) : null;
    }
}
