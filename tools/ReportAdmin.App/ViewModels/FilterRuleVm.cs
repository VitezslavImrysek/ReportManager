using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;

namespace ReportAdmin.App.ViewModels;

public sealed class FilterRuleVm : NotificationObject
{
	public IColumn? Column { get; set => SetValue(ref field, value); }
	public FilterOperation Operation { get; set => SetValue(ref field, value); } = FilterOperation.Eq;
    public required string ValuesText { get; set => SetValue(ref field, value); }
}
