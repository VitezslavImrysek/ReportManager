using ReportManager.ApiContracts.Dto;

namespace ReportAdmin.App.ViewModels;

public sealed class FilterRuleVm : NotificationObject
{
	public required string ColumnKey { get; set => SetValue(ref field, value); }
	public FilterOperation Operation { get; set => SetValue(ref field, value); } = FilterOperation.Eq;
    public required string ValuesText { get; set => SetValue(ref field, value); }
}
