using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;

namespace ReportAdmin.Core.Models.Definition;

public sealed class LookupConfigUi : NotificationObject
{
    public LookupMode Mode { get; set => SetValue(ref field, value); } = LookupMode.Sql;
    public SqlLookupUi? Sql { get; set => SetValue(ref field, value); }
    public ObservableCollection<LookupItemUi>? Items { get; set => SetValue(ref field, value); }

    public static explicit operator LookupConfigJson(LookupConfigUi ui)
    {
        if (ui == null) return null!;
        return new LookupConfigJson { Mode = ui.Mode, Sql = ui.Sql == null ? null : (SqlLookupJson)ui.Sql, Items = ui.Items?.Select(x => (LookupItemJson)x)?.ToList() ?? [] };
    }

    public static explicit operator LookupConfigUi(LookupConfigJson src)
    {
        if (src == null) return null!;
        var ui = new LookupConfigUi { Mode = src.Mode, Sql = src.Sql == null ? null : (SqlLookupUi)src.Sql };
        if (src.Items != null) ui.Items = new ObservableCollection<LookupItemUi>(src.Items.Select(x => (LookupItemUi)x).ToList());
        return ui;
    }
}
