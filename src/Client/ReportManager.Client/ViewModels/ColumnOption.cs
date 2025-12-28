using ReportManager.ApiContracts.Dto;
using System.Collections.ObjectModel;

namespace ReportManager.Client.ViewModels
{
	public sealed class ColumnOption : NotificationObject
	{
		public required string Key { get; set => SetValue(ref field, value); }
		public required string DisplayName { get; set => SetValue(ref field, value); }
		public bool IsHidden { get; set => SetValue(ref field, value); }
		public ReportColumnType Type { get; set => SetValue(ref field, value); }
		public required ObservableCollection<FilterOperation> Ops { get; set => SetValue(ref field, value); }
		public bool CanFilter { get; set => SetValue(ref field, value); }
		public bool CanSort { get; set => SetValue(ref field, value); }
		public bool HasLookup { get; set => SetValue(ref field, value); }
		public required ObservableCollection<LookupItemDto> LookupItems { get; set => SetValue(ref field, value); }

		public override string ToString() => DisplayName;
	}
}
