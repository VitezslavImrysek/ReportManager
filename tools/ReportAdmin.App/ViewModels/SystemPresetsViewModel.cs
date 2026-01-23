using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Preset;
using ReportAdmin.Core.Utils;
using ReportManager.Shared;
using System.Collections.ObjectModel;

namespace ReportAdmin.App.ViewModels
{
    public class SystemPresetsViewModel : DataEditorVM<ObservableCollection<SystemPresetUi>, object>
    {
        public SystemPresetsViewModel()
        {
            AddPresetCommand = new RelayCommand(AddPreset);
            RemovePresetCommand = new RelayCommand(RemovePreset);
        }

        #region Properties

        public ObservableCollection<SystemPresetUi> SystemPresets { get; set => SetValue(ref field, value); } = [];
        public SystemPresetUi? SelectedPreset { get; set => SetValue(ref field, value, OnSelectedPresetChanged); }

        public SystemPresetViewModel PresetVM { get; } = new();

        #endregion

        #region Commands

        public RelayCommand AddPresetCommand { get; }
        public RelayCommand RemovePresetCommand { get; }

        #endregion

        protected override void OnSetData(ObservableCollection<SystemPresetUi> data)
        {
            SystemPresets = data;
            SelectedPreset = data.FirstOrDefault();
        }

        protected override void OnGetData(ObservableCollection<SystemPresetUi> data)
        {
            foreach (var item in SystemPresets) 
            {
                if (string.IsNullOrWhiteSpace(item.PresetKey))
                {
                    throw new InvalidOperationException("PresetKey cannot be empty.");
                }
                item.PresetId = GuidUtil.FromPresetKey(item.PresetKey);

                data.Add(item);
            }
        }
        private void AddPreset()
        {
            var reportKey = SendMessage<GetReportKeyMessage>().ReportKey;
            var key = $"{reportKey}_{Guid.NewGuid():N}";
            var name = "New preset";
            var p = new SystemPresetUi
            {
                PresetKey = key,
                Name = name,
                IsDefault = SystemPresets.Count == 0,
                PresetId = GuidUtil.FromPresetKey(key),
                Content = new PresetContentUi()
            };
            p.Content.Texts[Constants.DefaultLanguage] = new Dictionary<string, string>
            {
                [KnownTextKeys.PresetTitle] = name
            };
            SystemPresets.Add(p);
            SelectedPreset = p;
            NotifyStatus("Preset added.");
        }

        private void RemovePreset()
        {
            if (SelectedPreset == null) return;
            SystemPresets.Remove(SelectedPreset);
            SelectedPreset = SystemPresets.FirstOrDefault();
            NotifyStatus("Preset removed.");
        }

        private void OnSelectedPresetChanged(SystemPresetUi? oldPreset, SystemPresetUi? newPreset)
        {
            if (oldPreset != null)
            {
                PresetVM.GetData(oldPreset);
            }

            if (newPreset != null)
            {
                PresetVM.SetData(newPreset);
            }
        }
    }
}
