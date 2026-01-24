using ReportAdmin.App.Messages;
using ReportAdmin.Core.Utils;
using ReportManager.DefinitionModel.Models.ReportPreset;
using ReportManager.Shared;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class PresetsViewModel : DataEditorVM<List<SystemPreset>>
    {
        public PresetsViewModel()
        {
            AddPresetCommand = new RelayCommand(AddPreset);
            RemovePresetCommand = new RelayCommand(RemovePreset);
        }

        #region Properties

        public ObservableCollection<PresetViewModel> SystemPresets { get; } = [];
        public PresetViewModel? SelectedPreset { get; set => SetValue(ref field, value); }

        #endregion

        #region Commands

        public RelayCommand AddPresetCommand { get; }
        public RelayCommand RemovePresetCommand { get; }

        #endregion

        protected override void OnSetData(List<SystemPreset> data)
        {
            SystemPresets.Clear();
            SelectedPreset = null;

            foreach (var preset in data)
            {
                var vm = new PresetViewModel();
                vm.SetData(preset);
                SystemPresets.Add(vm);
            }

            SelectedPreset = SystemPresets.FirstOrDefault();
        }

        protected override void OnGetData(List<SystemPreset> data)
        {
            foreach (var vm in SystemPresets) 
            {
                var preset = new SystemPreset();
                vm.GetData(preset);
                data.Add(preset);

                //if (string.IsNullOrWhiteSpace(item.PresetKey))
                //{
                //    throw new InvalidOperationException("PresetKey cannot be empty.");
                //}
                //item.PresetId = GuidUtil.FromPresetKey(item.PresetKey);
            }
        }
        private void AddPreset()
        {
            var reportKey = SendMessage<GetReportKeyMessage>().ReportKey;
            var key = $"{reportKey}_{Guid.NewGuid():N}";
            var name = "New preset";
            var p = new SystemPreset
            {
                PresetKey = key,
                IsDefault = SystemPresets.Count == 0,
                PresetId = GuidUtil.FromPresetKey(key),
                Content = new PresetContentJson()
            };
            p.Content.Texts[Constants.DefaultLanguage] = new Dictionary<string, string>
            {
                [KnownTextKeys.PresetTitle] = name
            };

            var vm = new PresetViewModel();
            vm.SetData(p);

            SystemPresets.Add(vm);
            SelectedPreset = vm;
            NotifyStatus("Preset added.");
        }

        private void RemovePreset()
        {
            if (SelectedPreset == null) return;
            SystemPresets.Remove(SelectedPreset);
            SelectedPreset = SystemPresets.FirstOrDefault();
            NotifyStatus("Preset removed.");
        }
    }
}
