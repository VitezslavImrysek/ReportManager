using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared.Dto;

namespace ReportAdmin.Core.Models.Definition;

public sealed class SortSpecUi : NotificationObject
{
    public string Column { get; set => SetValue(ref field, value); } = string.Empty;
    public SortDirection Dir { get; set => SetValue(ref field, value); }

    public static explicit operator SortSpecJson(SortSpecUi ui)
    {
        if (ui == null) return null!;
        return new SortSpecJson { Column = ui.Column, Dir = ui.Dir };
    }

    public static explicit operator SortSpecUi(SortSpecJson src)
    {
        if (src == null) return null!;
        return new SortSpecUi { Column = src.Column, Dir = src.Dir };
    }
}
