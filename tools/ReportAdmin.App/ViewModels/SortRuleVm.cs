using ReportAdmin.Core;
using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;

namespace ReportAdmin.App.ViewModels;

public sealed class SortRuleVm : NotificationObject
{
	public IColumn? Column { get; set => SetValue(ref field, value); }
	public SortDirection Direction { get; set => SetValue(ref field, value); } = SortDirection.Asc;
}
