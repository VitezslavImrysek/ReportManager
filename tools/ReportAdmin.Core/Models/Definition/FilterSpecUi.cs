using ReportManager.DefinitionModel.Models.ReportPreset;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models.Definition;

public sealed class FilterSpecUi : NotificationObject
{
    public string ColumnKey { get; set => SetValue(ref field, value); } = string.Empty;
    public ObservableCollection<string> Values { get; set => SetValue(ref field, value); } = new();
    public FilterOperation Operation { get; set => SetValue(ref field, value); } = FilterOperation.Eq;

    public static explicit operator FilterSpecJson(FilterSpecUi ui)
    {
        if (ui == null) return null!;
        return new FilterSpecJson { ColumnKey = ui.ColumnKey, Operation = ui.Operation, Values = ui.Values.ToList() };
    }

    public static explicit operator FilterSpecUi(FilterSpecJson src)
    {
        if (src == null) return null!;
        return new FilterSpecUi { ColumnKey = src.ColumnKey, Operation = src.Operation, Values = new ObservableCollection<string>(src.Values) };
    }
}
