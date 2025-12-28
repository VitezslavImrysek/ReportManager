using ReportManager.ApiContracts.Dto;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{

	public sealed class QueryConditionViewModel : NotificationObject
	{
		public bool IsLookupColumn => SelectedColumn?.HasLookup == true;

		public Visibility LookupSingleVisibility =>
			IsLookupColumn && (SelectedOp == FilterOperation.Eq || SelectedOp == FilterOperation.Ne)
				? Visibility.Visible
				: Visibility.Collapsed;

		public Visibility LookupMultiVisibility =>
			IsLookupColumn && (SelectedOp == FilterOperation.In || SelectedOp == FilterOperation.NotIn)
				? Visibility.Visible
				: Visibility.Collapsed;

		public Visibility TextValue1Visibility =>
			IsLookupColumn ? Visibility.Collapsed : Visibility.Visible;

		// Between Value2 jen pro non-lookup sloupce
		public Visibility BetweenValue2Visibility =>
			(!IsLookupColumn && SelectedOp == FilterOperation.Between)
				? Visibility.Visible
				: Visibility.Collapsed;

		public required ObservableCollection<ColumnOption> AvailableColumns { get; set => SetValue(ref field, value); }
		public ObservableCollection<FilterOperation> AvailableOps { get; set => SetValue(ref field, value); } = [];

		public ColumnOption? SelectedColumn
		{
			get;
			set
			{
				SetValue(ref field, value);

				AvailableOps = value?.Ops ?? [];

				OnPropertyChanged(nameof(IsLookupColumn));
				OnPropertyChanged(nameof(LookupSingleVisibility));
				OnPropertyChanged(nameof(LookupMultiVisibility));
				OnPropertyChanged(nameof(TextValue1Visibility));
				OnPropertyChanged(nameof(BetweenValue2Visibility));

				// vyber první op
				if (AvailableOps.Count > 0)
					SelectedOp = AvailableOps[0];

				// reset hodnot při změně sloupce
				Value1 = string.Empty;
				Value2 = string.Empty;
				SelectedLookupItem = null;
			}
		}

		public FilterOperation SelectedOp
		{
			get;
			set
			{
				SetValue(ref field, value);
				OnPropertyChanged(nameof(LookupSingleVisibility));
				OnPropertyChanged(nameof(LookupMultiVisibility));
				OnPropertyChanged(nameof(TextValue1Visibility));
				OnPropertyChanged(nameof(BetweenValue2Visibility));

				// když přepneš z lookup na non-lookup nebo op, vyčisti hodnoty, ať tam nezůstane bordel
				if (IsLookupColumn)
				{
					if (SelectedOp == FilterOperation.IsNull || SelectedOp == FilterOperation.NotNull)
					{
						SelectedLookupItem = null;
						Value1 = string.Empty;
						Value2 = string.Empty;
					}
				}
			}
		}

		public LookupItemDto? SelectedLookupItem
		{
			get;
			set
			{
				SetValue(ref field, value);

				// pro Eq/Ne držíme v Value1 "Key"
				if (value != null)
					Value1 = value.Key ?? string.Empty;
			}
		}

		public string Value1 { get; set => SetValue(ref field, value); } = string.Empty;
		public string Value2 { get; set => SetValue(ref field, value); } = string.Empty;

		public ICommand? RemoveCommand { get; set; }

		public List<string> GetValuesForDto()
		{
			if (SelectedOp == FilterOperation.In || SelectedOp == FilterOperation.NotIn)
			{
				// split by comma/semicolon/newline/space
				var raw = (Value1 ?? string.Empty);
				var parts = raw
					.Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries)
					.Select(x => x.Trim())
					.Where(x => x.Length > 0)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.ToList();
				return parts;
			}

			if (SelectedOp == FilterOperation.Between)
				return new List<string> { Value1 ?? string.Empty, Value2 ?? string.Empty };

			if (SelectedOp == FilterOperation.IsNull || SelectedOp == FilterOperation.NotNull)
				return new List<string>();

			return new List<string> { Value1 ?? string.Empty };
		}
	}
}
