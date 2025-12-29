using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.Globalization;
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

		public bool TryGetValuesForDto(out List<string> values, out string? error)
		{
			values = new List<string>();
			error = null;

			if (SelectedColumn == null)
			{
				error = "No column selected.";
				return false;
			}

			if (SelectedOp == FilterOperation.In || SelectedOp == FilterOperation.NotIn)
			{
				var parts = GetValuesForDto();
				if (parts.Count == 0)
				{
					error = $"Column '{SelectedColumn.DisplayName}': missing value.";
					return false;
				}

				foreach (var part in parts)
				{
					if (!IsValidValue(SelectedColumn.Type, part))
					{
						error = $"Column '{SelectedColumn.DisplayName}': '{part}' is not a valid {SelectedColumn.Type}.";
						return false;
					}
				}

				values = parts;
				return true;
			}

			if (SelectedOp == FilterOperation.Between)
			{
				if (string.IsNullOrWhiteSpace(Value1) || string.IsNullOrWhiteSpace(Value2))
				{
					error = $"Column '{SelectedColumn.DisplayName}': both values are required.";
					return false;
				}

				if (!IsValidValue(SelectedColumn.Type, Value1))
				{
					error = $"Column '{SelectedColumn.DisplayName}': '{Value1}' is not a valid {SelectedColumn.Type}.";
					return false;
				}

				if (!IsValidValue(SelectedColumn.Type, Value2))
				{
					error = $"Column '{SelectedColumn.DisplayName}': '{Value2}' is not a valid {SelectedColumn.Type}.";
					return false;
				}

				values = new List<string> { Value1, Value2 };
				return true;
			}

			if (SelectedOp == FilterOperation.IsNull || SelectedOp == FilterOperation.NotNull)
			{
				return true;
			}

			if (string.IsNullOrWhiteSpace(Value1))
			{
				error = $"Column '{SelectedColumn.DisplayName}': value is required.";
				return false;
			}

			if (!IsValidValue(SelectedColumn.Type, Value1))
			{
				error = $"Column '{SelectedColumn.DisplayName}': '{Value1}' is not a valid {SelectedColumn.Type}.";
				return false;
			}

			values = new List<string> { Value1 };
			return true;
		}

		private static bool IsValidValue(ReportColumnType type, string raw)
		{
			if (raw == null) return false;
			raw = raw.Trim();

			switch (type)
			{
				case ReportColumnType.Int32:
					return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
				case ReportColumnType.Int64:
					return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);
				case ReportColumnType.Decimal:
					return decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
				case ReportColumnType.Double:
					return double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
				case ReportColumnType.Bool:
					return bool.TryParse(raw, out _);
				case ReportColumnType.Guid:
					return Guid.TryParse(raw, out _);
				case ReportColumnType.Date:
				case ReportColumnType.DateTime:
					return DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out _);
				case ReportColumnType.String:
				default:
					return true;
			}
		}
	}
}
