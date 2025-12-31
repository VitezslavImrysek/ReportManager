using ReportManager.DefinitionModel.Models.ReportDefinition;

namespace ReportAdmin.Core.Models.Definition;

public sealed class SqlLookupUi : NotificationObject
{
    public string CommandText { get; set => SetValue(ref field, value); } = string.Empty;
    public string KeyColumn { get; set => SetValue(ref field, value); } = "Id";
    public string TextColumn { get; set => SetValue(ref field, value); } = "Name";

    public static explicit operator SqlLookupJson(SqlLookupUi ui)
    {
        if (ui == null) return null!;
        return new SqlLookupJson { CommandText = ui.CommandText, KeyColumn = ui.KeyColumn, TextColumn = ui.TextColumn };
    }

    public static explicit operator SqlLookupUi(SqlLookupJson src)
    {
        if (src == null) return null!;
        return new SqlLookupUi { CommandText = src.CommandText, KeyColumn = src.KeyColumn, TextColumn = src.TextColumn };
    }
}
