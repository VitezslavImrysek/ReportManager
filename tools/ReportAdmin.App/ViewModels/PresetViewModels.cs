using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Preset;
using ReportManager.Shared;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels;

/// <summary>
/// UI editor for PresetContentJson.
/// </summary>
public sealed class PresetViewModel : DataEditorVM<SystemPresetUi, object>
{
    #region Ctor

    public PresetViewModel()
    {
        FilterVM = new FilterViewModel();
        SortVM = new SortViewModel();
        PresetsTextsVM = new TextsViewModel() { Mode = TextsEditorMode.Preset };

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

        RegisterMessage<ColumnChangedMessage>(OnColumnChanged);
    }

    #endregion

    #region Properties

    public Guid PresetId { get; set => SetValue(ref field, value); }
    public string? PresetKey { get; set => SetValue(ref field, value); }
    public bool IsDefault { get; set => SetValue(ref field, value); }
    public string? Name { get; set => SetValue(ref field, value); }

    #endregion

    public ObservableCollection<ColumnVisibilityRowVm> Columns { get; } = new();
	
    #region View Models

    public SortViewModel SortVM { get; set => SetValue(ref field, value); }
    public FilterViewModel FilterVM { get; set => SetValue(ref field, value); }
    public TextsViewModel PresetsTextsVM { get; set => SetValue(ref field, value); }

    #endregion

    #region Commands

	public RelayCommand ShowAllColumnsCommand { get; }
	public RelayCommand HideAllColumnsCommand { get; }

    #endregion

    protected override void OnSetData(SystemPresetUi data)
    {
        Columns.Clear();

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

        FilterVM.SetData(data.Content.Query.Filters);
        SortVM.SetData(data.Content.Query.Sorting);

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
        FilterVM.GetData(filters);

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

    private void OnColumnChanged(ColumnChangedMessage message)
    {
        switch (message.ChangeKind)
        {
            case ColumnChangeKind.Added:
                break;
            case ColumnChangeKind.Deleted:
                break;
            case ColumnChangeKind.Changed:
                break;
            default:
                break;
        }
    }

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
		ShowAllColumnsCommand.RaiseCanExecuteChanged();
		HideAllColumnsCommand.RaiseCanExecuteChanged();
	}
}