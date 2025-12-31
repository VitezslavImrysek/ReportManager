using ReportManager.DefinitionModel.Models.ReportDefinition;

namespace ReportAdmin.Core.Models.Definition;

public sealed class LookupItemUi : NotificationObject
{
    public string Key { get; set => SetValue(ref field, value); } = string.Empty;
    public string Text { get; set => SetValue(ref field, value); } = string.Empty;

    public static explicit operator LookupItemJson(LookupItemUi ui)
    {
        if (ui == null) return null!;
        return new LookupItemJson { Key = ui.Key, Text = ui.Text };
    }

    public static explicit operator LookupItemUi(LookupItemJson src)
    {
        if (src == null) return null!;
        return new LookupItemUi { Key = src.Key, Text = src.Text };
    }
}
