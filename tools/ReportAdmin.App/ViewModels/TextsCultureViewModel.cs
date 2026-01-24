using System.Collections.ObjectModel;
using System.Text;

namespace ReportAdmin.App.ViewModels
{
    public class TextsCultureViewModel : DataEditorVM<Dictionary<string, string>>
    {
        public string CultureName { get; set => SetValue(ref field, value); } = string.Empty;
        public ObservableCollection<TextEntryViewModel> Texts { get; set => SetValue(ref field, value); } = [];

        protected override void OnNew(object? context)
        {
            Texts.Clear();
        }

        protected override void OnGetData(Dictionary<string, string> data)
        {
            foreach (var textVm in Texts)
            {
                data[textVm.Key!] = textVm.Value!;
            }
        }

        protected override void OnSetData(Dictionary<string, string> data)
        {
            Texts.Clear();
            foreach (var kvp in data)
            {
                var vm = new TextEntryViewModel() { Key = kvp.Key, Value = kvp.Value };
                Texts.Add(vm);
            }
        }

        protected override bool OnValidate(StringBuilder log)
        {
            var isOK = base.OnValidate(log);

            foreach (var textVm in Texts)
            {
                if (string.IsNullOrWhiteSpace(textVm.Key))
                {
                    log.AppendLine("A text entry has an empty key.");
                    isOK = false;
                }

                if (textVm.Value == null)
                {
                    log.AppendLine($"The text entry with key '{textVm.Key}' has a null value.");
                    isOK = false;
                }
            }

            return isOK;
        }
    }
}
