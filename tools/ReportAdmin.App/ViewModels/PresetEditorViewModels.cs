using ReportAdmin.App.Extensions;
using ReportAdmin.Core;
using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Models.Preset;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ReportAdmin.App.ViewModels;

/// <summary>
/// UI editor for PresetContentJson.
/// </summary>
public sealed class PresetEditorViewModel : NotificationObject
{
	private ReportDefinitionUi? _definition;
	private SystemPresetUi? _preset;

    public PresetEditorViewModel()
    {
        AddSortCommand = new RelayCommand(AddSort, () => _definition != null);
        RemoveSortCommand = new RelayCommand(RemoveSort, () => SelectedSort != null);
        MoveSortUpCommand = new RelayCommand(() => MoveSort(-1), () => SelectedSort != null);
        MoveSortDownCommand = new RelayCommand(() => MoveSort(1), () => SelectedSort != null);

        AddFilterCommand = new RelayCommand(AddFilter, () => _definition != null);
        RemoveFilterCommand = new RelayCommand(RemoveFilter, () => SelectedFilter != null);

        ShowAllColumnsCommand = new RelayCommand(() =>
        {
            foreach (var c in Columns.Where(x => x.CanToggle))
                c.IsVisible = true;
        }, () => _definition != null);

        HideAllColumnsCommand = new RelayCommand(() =>
        {
            foreach (var c in Columns.Where(x => x.CanToggle))
                c.IsVisible = false;
        }, () => _definition != null);
    }

    public ObservableCollection<ColumnVisibilityRowVm> Columns { get; } = new();
	public ObservableCollection<SortRuleVm> Sorting { get; } = new();
	public ObservableCollection<FilterRuleVm> Filters { get; } = new();

	public ObservableCollection<ReportColumnUi> FilterableColumns { get; } = new();
	public ObservableCollection<ReportColumnUi> SortableColumns { get; } = new();

	public ObservableCollection<SortDirection> SortDirectionValues { get; } = new(Enum.GetValues(typeof(SortDirection)).Cast<SortDirection>());
	public ObservableCollection<FilterOperation> FilterOperationValues { get; } = new(Enum.GetValues(typeof(FilterOperation)).Cast<FilterOperation>());

	private SortRuleVm? _selectedSort;
	public SortRuleVm? SelectedSort { get => _selectedSort; set => SetValue(ref _selectedSort, value); }

	private FilterRuleVm? _selectedFilter;
	public FilterRuleVm? SelectedFilter { get => _selectedFilter; set => SetValue(ref _selectedFilter, value); }

	public RelayCommand AddSortCommand { get; }
	public RelayCommand RemoveSortCommand { get; }
	public RelayCommand MoveSortUpCommand { get; }
	public RelayCommand MoveSortDownCommand { get; }

	public RelayCommand AddFilterCommand { get; }
	public RelayCommand RemoveFilterCommand { get; }

	public RelayCommand ShowAllColumnsCommand { get; }
	public RelayCommand HideAllColumnsCommand { get; }

	public void Load(ReportDefinitionUi? definition, SystemPresetUi? preset)
	{
		_definition = definition;
		_preset = preset;

		Columns.Clear();
		Sorting.Clear();
		Filters.Clear();
		FilterableColumns.Clear();
		SortableColumns.Clear();

		if (definition == null || preset == null)
		{
			RaiseCanExec();
			return;
		}

		var hidden = new HashSet<string>(preset.Content.Grid.HiddenColumns, StringComparer.OrdinalIgnoreCase);

		foreach (var col in definition.Columns)
		{
			var caption = ResolveCaption(definition, KnownTextKeys.GetColumnHeaderKey(col.Key), col.Key);
			var canToggle = !col.AlwaysSelect && !col.Hidden;

			Columns.Add(new ColumnVisibilityRowVm
			{
				Key = col.Key,
				Caption = caption,
				CanToggle = canToggle,
				IsVisible = !hidden.Contains(col.Key) || !canToggle
			});
		}

		foreach (var col in definition.Columns.Where(c => c.Filterable))
			FilterableColumns.Add(col);

		foreach (var col in definition.Columns.Where(c => c.Sortable))
			SortableColumns.Add(col);

		foreach (var s in preset.Content.Query.Sorting)
			Sorting.Add(new SortRuleVm { ColumnKey = s.ColumnKey, Direction = s.Direction });

		foreach (var f in preset.Content.Query.Filters)
		{
			var vm = new FilterRuleVm
			{
				ColumnKey = f.ColumnKey,
				Operation = f.Operation,
				ValuesText = string.Join(Environment.NewLine, f.Values)
			};
			vm.PropertyChanged += FilterVm_PropertyChanged;
			Filters.Add(vm);
		}

		RaiseCanExec();
	}

	public PresetContentUi BuildContent()
	{
		if (_definition == null || _preset == null)
			return new PresetContentUi();

		var hidden = Columns
			.Where(c => c.CanToggle && !c.IsVisible)
			.Select(c => c.Key)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToObservable();

		var sorting = new ObservableCollection<Core.Models.Preset.SortSpecUi>(
			Sorting
				.Where(s => !string.IsNullOrWhiteSpace(s.ColumnKey))
				.Select(s => new Core.Models.Preset.SortSpecUi { ColumnKey = s.ColumnKey, Direction = s.Direction })
		);

		var filters = new ObservableCollection<Core.Models.Preset.FilterSpecUi>();
		foreach (var f in Filters)
		{
			if (string.IsNullOrWhiteSpace(f.ColumnKey)) continue;
			var values = ParseValues(f.ValuesText);

			if (f.Operation is FilterOperation.IsNull or FilterOperation.NotNull)
				values.Clear();

			if (f.Operation is FilterOperation.Between)
			{
				if (values.Count < 2) continue;
				values = values.Take(2).ToList();
			}

			if (RequiresValues(f.Operation) && values.Count == 0)
				continue;

			filters.Add(new Core.Models.Preset.FilterSpecUi
			{
				ColumnKey = f.ColumnKey,
				Operation = f.Operation,
				Values = values.ToObservable()
			});
		}

		return new PresetContentUi
		{
			Version = _preset.Content?.Version ?? 1,
			Grid = new GridStateUi
            {
				HiddenColumns = hidden,
				Order = _preset.Content?.Grid?.Order ?? []
			},
			Query = new QuerySpecUi
            {
				Filters = filters,
				Sorting = sorting,
				SelectedColumns = _preset.Content?.Query?.SelectedColumns ?? []
			}
		};
	}

	private void AddSort()
	{
		var first = SortableColumns.FirstOrDefault();
		Sorting.Add(new SortRuleVm { ColumnKey = first?.Key ?? "", Direction = SortDirection.Asc });
		SelectedSort = Sorting.LastOrDefault();
		RaiseCanExec();
	}

	private void RemoveSort()
	{
		if (SelectedSort == null) return;
		Sorting.Remove(SelectedSort);
		SelectedSort = Sorting.LastOrDefault();
		RaiseCanExec();
	}

	private void MoveSort(int delta)
	{
		if (SelectedSort == null) return;
		var idx = Sorting.IndexOf(SelectedSort);
		var nidx = idx + delta;
		if (nidx < 0 || nidx >= Sorting.Count) return;
		Sorting.Move(idx, nidx);
		RaiseCanExec();
	}

	private void AddFilter()
	{
		var first = FilterableColumns.FirstOrDefault();
		var vm = new FilterRuleVm { ColumnKey = first?.Key ?? "", Operation = FilterOperation.Eq, ValuesText = "" };
		vm.PropertyChanged += FilterVm_PropertyChanged;
		Filters.Add(vm);
		SelectedFilter = vm;
		RaiseCanExec();
	}

	private void RemoveFilter()
	{
		if (SelectedFilter == null) return;
		SelectedFilter.PropertyChanged -= FilterVm_PropertyChanged;
		Filters.Remove(SelectedFilter);
		SelectedFilter = Filters.LastOrDefault();
		RaiseCanExec();
	}

	private void FilterVm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(FilterRuleVm.Operation))
		{
			if (sender is FilterRuleVm vm && (vm.Operation is FilterOperation.IsNull or FilterOperation.NotNull))
				vm.ValuesText = "";
		}
	}

	private static bool RequiresValues(FilterOperation op) =>
		op is not (FilterOperation.IsNull or FilterOperation.NotNull);

	private static List<string> ParseValues(string? text)
		=> (text ?? "")
			.Split(["\r\n", "\n"], StringSplitOptions.None)
			.Select(x => (x ?? "").Trim())
			.Where(x => x.Length > 0)
			.ToList();

	private static string ResolveCaption(ReportDefinitionUi def, string textKey, string fallback)
	{
		if (def.Texts.TryGetValue(def.DefaultCulture, out var dict) &&
			dict.TryGetValue(textKey, out var s) &&
			!string.IsNullOrWhiteSpace(s))
			return s;

		foreach (var d in def.Texts.Values)
			if (d.TryGetValue(textKey, out var s2) && !string.IsNullOrWhiteSpace(s2))
				return s2;

		return fallback;
	}

	private void RaiseCanExec()
	{
		AddSortCommand.RaiseCanExecuteChanged();
		RemoveSortCommand.RaiseCanExecuteChanged();
		MoveSortUpCommand.RaiseCanExecuteChanged();
		MoveSortDownCommand.RaiseCanExecuteChanged();
		AddFilterCommand.RaiseCanExecuteChanged();
		RemoveFilterCommand.RaiseCanExecuteChanged();
		ShowAllColumnsCommand.RaiseCanExecuteChanged();
		HideAllColumnsCommand.RaiseCanExecuteChanged();
	}
}