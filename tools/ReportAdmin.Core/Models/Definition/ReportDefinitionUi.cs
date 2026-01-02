using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models.Definition;

public sealed class ReportDefinitionUi : NotificationObject
{
    public int Version { get; set => SetValue(ref field, value); } = 1;
    public string DefaultCulture { get; set => SetValue(ref field, value); } = Constants.DefaultLanguage;
    public Dictionary<string, Dictionary<string, string>> Texts { get; set => SetValue(ref field, value); } = new(StringComparer.OrdinalIgnoreCase);
    public ObservableCollection<ReportColumnUi> Columns { get; set => SetValue(ref field, value); } = new();
    public ObservableCollection<SortSpecUi> DefaultSort { get; set => SetValue(ref field, value); } = new();

    public static explicit operator ReportDefinitionJson(ReportDefinitionUi ui)
    {
        if (ui == null) return null!;
        var r = new ReportDefinitionJson
        {
            Version = ui.Version,
            DefaultCulture = ui.DefaultCulture,
            Texts = ui.Texts,
            Columns = [],
            DefaultSort = []
        };

        foreach (var c in ui.Columns)
            r.Columns.Add((ReportColumnJson)c);

        foreach (var s in ui.DefaultSort)
            r.DefaultSort.Add((SortSpecJson)s);

        return r;
    }

    public static explicit operator ReportDefinitionUi(ReportDefinitionJson src)
    {
        if (src == null) return null!;
        var ui = new ReportDefinitionUi
        {
            Version = src.Version,
            DefaultCulture = src.DefaultCulture,
            Texts = src.Texts ?? new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase),
            Columns = [],
            DefaultSort = []
        };

        ui.Columns.Clear();
        if (src.Columns != null)
        {
            foreach (var c in src.Columns)
                ui.Columns.Add((ReportColumnUi)c);
        }

        ui.DefaultSort.Clear();
        if (src.DefaultSort != null)
        {
            foreach (var s in src.DefaultSort)
                ui.DefaultSort.Add((SortSpecUi)s);
        }

        return ui;
    }
}
