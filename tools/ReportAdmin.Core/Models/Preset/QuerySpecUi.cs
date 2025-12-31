using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models.Preset;

public sealed class QuerySpecUi : NotificationObject
{
    public ObservableCollection<FilterSpecUi> Filters { get; set => SetValue(ref field, value); } = new();
    public ObservableCollection<SortSpecUi> Sorting { get; set => SetValue(ref field, value); } = new();
    public ObservableCollection<string> SelectedColumns { get; set => SetValue(ref field, value); } = new();

    public static explicit operator QuerySpecJson(QuerySpecUi ui)
    {
        if (ui == null) return null!;
        return new QuerySpecJson { Filters = ui.Filters.Select(f => (FilterSpecJson)f).ToList(), Sorting = ui.Sorting.Select(s => (SortSpecJson)s).ToList(), SelectedColumns = ui.SelectedColumns.ToList() };
    }

    public static explicit operator QuerySpecUi(QuerySpecJson src)
    {
        if (src == null) return null!;
        var ui = new QuerySpecUi()
        {
            Filters = new ObservableCollection<FilterSpecUi>(src.Filters.Select(f => (FilterSpecUi)f)),
            Sorting = new ObservableCollection<SortSpecUi>(src.Sorting.Select(s => (SortSpecUi)s)),
            SelectedColumns = new ObservableCollection<string>(src.SelectedColumns)
        };
        return ui;
    }
}
