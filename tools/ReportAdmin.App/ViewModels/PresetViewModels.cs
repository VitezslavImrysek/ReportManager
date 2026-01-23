using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
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
public sealed class PresetViewModel : DataEditorVM<SystemPresetUi, object>
{
    public PresetViewModel()
    {
        SortVM = new SortViewModel();
        PresetsTextsVM = new TextsViewModel() { Mode = TextsEditorMode.Preset };

        AddFilterCommand = new RelayCommand(AddFilter);
        RemoveFilterCommand = new RelayCommand(RemoveFilter, () => SelectedFilter != null);

        ShowAllColumnsCommand = new RelayCommand(() =>
        {
            foreach (var c in Columns)
                c.IsVisible = true;
        });

        HideAllColumnsCommand = new RelayCommand(() =>
        {
            foreach (var c in Columns)
                c.IsVisible = false;
        });
    }

    public Guid PresetId { get; set => SetValue(ref field, value); }
    public string? PresetKey { get; set => SetValue(ref field, value); }
    public bool IsDefault { get; set => SetValue(ref field, value); }
    public string? Name { get; set => SetValue(ref field, value); }

    public ObservableCollection<ColumnVisibilityRowVm> Columns { get; } = new();
	public ObservableCollection<FilterRuleVm> Filters { get; } = new();

	public ObservableCollection<ReportColumnUi> FilterableColumns { get; } = new();
	public ObservableCollection<FilterOperation> FilterOperationValues { get; } = new(Enum.GetValues(typeof(FilterOperation)).Cast<FilterOperation>());

	public FilterRuleVm? SelectedFilter { get; set => SetValue(ref field, value); }

    public SortViewModel SortVM { get; set => SetValue(ref field, value); }
    public TextsViewModel PresetsTextsVM { get; set => SetValue(ref field, value); }

	public RelayCommand AddFilterCommand { get; }
	public RelayCommand RemoveFilterCommand { get; }

	public RelayCommand ShowAllColumnsCommand { get; }
	public RelayCommand HideAllColumnsCommand { get; }

    private List<string> _columnsOrder = [];

    protected override void OnSetData(SystemPresetUi data)
    {
        // Should keep both Hidden and Selected columns? More like no
        Columns.Clear();
        Filters.Clear();
        FilterableColumns.Clear();

        if (data == null)
        {
            RaiseCanExec();
            return;
        }

        PresetKey = data.PresetKey;
        IsDefault = data.IsDefault;
        Name = data.Name;

        var hidden = new HashSet<string>(data.Content.Grid.HiddenColumns, StringComparer.OrdinalIgnoreCase);

        var msg = SendMessage<GetColumnsMessage>();
        foreach (var col in msg.Columns)
        {
            if (col.Hidden)
            {
                continue;
            }

            var caption = ResolveColumnCaption(KnownTextKeys.GetColumnHeaderKey(col.Key), col.Key);

            Columns.Add(new ColumnVisibilityRowVm
            {
                Key = col.Key,
                Caption = caption,
                IsVisible = !hidden.Contains(col.Key)
            });
        }

        foreach (var col in msg.Columns.Where(c => c.Filterable && c.Filter?.Hidden == false))
            FilterableColumns.Add(col);

        SortVM.SetData(data.Content.Query.Sorting);

        foreach (var f in data.Content.Query.Filters)
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

        PresetsTextsVM.DefaultCulture = SendMessage<GetCultureMessage>().Culture;
        PresetsTextsVM.SetData(data.Content.Texts);

        RaiseCanExec();
    }

    protected override void OnGetData(SystemPresetUi data)
    {
        var hidden = Columns
            .Where(c => !c.IsVisible)
            .Select(c => c.Key)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToObservable();

        var sorting = new ObservableCollection<Core.Models.SortSpecUi>();
        SortVM.GetData(sorting);

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

        data.Content = new PresetContentUi
        {
            Grid = new GridStateUi
            {
                HiddenColumns = hidden,
                Order = []
            },
            Query = new QuerySpecUi
            {
                Filters = filters,
                Sorting = sorting,
                // If all columns are visible, then dont set selected columns.
                SelectedColumns = Columns.All(x => x.IsVisible) ? [] : Columns.Where(x => x.IsVisible).Select(x => x.Key).ToObservable() 
            }
        };
        data.IsDefault = IsDefault;
        data.PresetId = PresetId;
        data.PresetKey = PresetKey;

        data.Name = PresetsTextsVM.Title;
        data.Content.Texts = new Dictionary<string, Dictionary<string, string>>();
        PresetsTextsVM.GetData(data.Content.Texts);
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

	private string ResolveColumnCaption(string textKey, string fallback)
	{
        var culture = SendMessage<GetCultureMessage>().Culture;
        var msg = SendMessage(new ResolveTextKeyMessage() 
        {
            Culture = culture,
            Key = textKey,
            TextsEditorMode = TextsEditorMode.Report
        });

        return msg.Value ?? fallback;
	}

	private void RaiseCanExec()
	{
		AddFilterCommand.RaiseCanExecuteChanged();
		RemoveFilterCommand.RaiseCanExecuteChanged();
		ShowAllColumnsCommand.RaiseCanExecuteChanged();
		HideAllColumnsCommand.RaiseCanExecuteChanged();
	}
}