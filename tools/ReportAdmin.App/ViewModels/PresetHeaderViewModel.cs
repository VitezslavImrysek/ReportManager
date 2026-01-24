using ReportManager.DefinitionModel.Models.ReportPreset;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ReportAdmin.App.ViewModels
{
    public class PresetHeaderViewModel : DataEditorVM<SystemPreset>
    {
        #region Properties

        public Guid PresetId { get; set => SetValue(ref field, value); }

        [Required]
        [Description("Preset key")]
        public string PresetKey { get; set => SetValue(ref field, value); } = string.Empty;

        public bool IsDefault { get; set => SetValue(ref field, value); }

        public string? Name { get; set => SetValue(ref field, value); }

        #endregion


        #region Override Methods

        protected override void OnGetData(SystemPreset data)
        {
            data.IsDefault = IsDefault;
            data.PresetId = PresetId;
            data.PresetKey = PresetKey!;
        }

        protected override void OnSetData(SystemPreset data)
        {
            IsDefault = data.IsDefault;
            PresetId = data.PresetId;
            PresetKey = data.PresetKey;
        }

        #endregion
    }
}
