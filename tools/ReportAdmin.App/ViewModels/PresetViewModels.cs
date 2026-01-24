using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Preset;
using ReportManager.DefinitionModel.Models.ReportPreset;

namespace ReportAdmin.App.ViewModels;

/// <summary>
/// UI editor for PresetContentJson.
/// </summary>
public sealed class PresetViewModel : DataEditorVM<SystemPresetUi>
{
    #region View Models

    public PresetHeaderViewModel HeaderVM { get; } = new PresetHeaderViewModel();
    public ColumnsVisibilityViewModel ColumnsVM { get; } = new ColumnsVisibilityViewModel();
    public SortViewModel SortVM { get; } = new SortViewModel();
    public FilterViewModel FilterVM { get; } = new FilterViewModel();
    public TextsViewModel TextsVM { get; } = new TextsViewModel() { Mode = TextsEditorMode.Preset };

    #endregion

    protected override void OnSetData(SystemPresetUi data)
    {
        HeaderVM.SetData(data);
        ColumnsVM.SetData(data.Content.Grid.HiddenColumns);
        FilterVM.SetData(data.Content.Query.Filters);
        SortVM.SetData(data.Content.Query.Sorting);

        TextsVM.DefaultCulture = SendMessage<GetCultureMessage>().Culture;
        TextsVM.SetData(data.Content.Texts);
    }

    protected override void OnGetData(SystemPresetUi data)
    {
        HeaderVM.GetData(data);
        data.Name = TextsVM.Title;

        data.Content = new PresetContentJson
        {
            Grid = new GridStateJson
            {
                HiddenColumns = [],
                Order = []
            },
            Query = new QuerySpecJson
            {
                Filters = [],
                Sorting = [],
                // If all columns are visible, then dont set selected columns.
                //SelectedColumns = Columns.All(x => x.IsVisible) ? [] : Columns.Where(x => x.IsVisible).Select(x => x.Key).ToObservable() 
            },
            Texts = []
        };

        ColumnsVM.GetData(data.Content.Grid.HiddenColumns);
        SortVM.GetData(data.Content.Query.Sorting);
        FilterVM.GetData(data.Content.Query.Filters);
        TextsVM.GetData(data.Content.Texts);
    }
}