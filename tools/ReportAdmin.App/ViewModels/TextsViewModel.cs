using ReportAdmin.App.Messages;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Shared;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ReportAdmin.App.ViewModels
{
    public class TextsViewModel : DataEditorVM<Dictionary<string, Dictionary<string, string>>>
    {
        public TextsViewModel()
        {
            RegisterMessage<ColumnChangedMessage>(OnColumnChangedMessageReceived);
            RegisterMessage<CultureChangedMessage>(OnCultureChangedMessageReceived);
            RegisterMessage<ResolveTextKeyMessage>(OnResolveTextKeyMessageReceived);

            AddCultureCommand = new RelayCommand(AddCulture);
            RemoveCultureCommand = new RelayCommand(RemoveCulture);
            RegenerateTextsCommand = new RelayCommand(RegenerateAll);
        }

        public RelayCommand AddCultureCommand { get; set => SetValue(ref field, value); }
        public RelayCommand RemoveCultureCommand { get; set => SetValue(ref field, value); }
        public RelayCommand RegenerateTextsCommand { get; set => SetValue(ref field, value); }

        public TextsEditorMode Mode { get; init; }
        public string DefaultCulture { get; set => SetValue(ref field, value, OnDefaultCultureChanged); } = Constants.DefaultLanguage;
        public string Title
        {
            get 
            {
                var textKey = Mode == TextsEditorMode.Report ? KnownTextKeys.ReportTitle : KnownTextKeys.PresetTitle;
                return ResolveText(textKey, DefaultCulture);
            }
        }

        public ObservableCollection<TextsCultureViewModel> CultureTexts { get; } = [];
        public TextsCultureViewModel? SelectedCultureText { get; set => SetValue(ref field, value); }

        public string SelectedCultureTitle => SelectedCultureText == null ? "No culture selected" : $"Culture: {SelectedCultureText.CultureName}";

        protected override void OnNew(object? context)
        {
            
        }

        protected override void OnSetData(Dictionary<string, Dictionary<string, string>> data)
        {
            CultureTexts.Clear();
            foreach (var cultureTexts in data)
            {
                var vm = new TextsCultureViewModel()
                {
                    CultureName = cultureTexts.Key
                };
                vm.SetData(cultureTexts.Value);
                CultureTexts.Add(vm);
            }

            SelectedCultureText = CultureTexts.FirstOrDefault();
        }

        protected override void OnGetData(Dictionary<string, Dictionary<string, string>> data)
        {
            foreach (var cultureTextsVM in CultureTexts)
            {
                var dict = new Dictionary<string, string>();
                cultureTextsVM.GetData(dict);
                data[cultureTextsVM.CultureName] = dict;
            }
        }

        private TextsCultureViewModel EnsureCulture(string key)
        {
            var vm = CultureTexts.FirstOrDefault(x => x.CultureName.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (vm == null)
            {
                vm = new TextsCultureViewModel()
                {
                    CultureName = key
                };
                vm.SetData([]);
                CultureTexts.Add(vm);
            }

            return vm;
        }

        private void AddCulture()
        {
            var key = Microsoft.VisualBasic.Interaction.InputBox("Culture key (e.g. cs, en, pl):", "Add culture", "en").Trim();
            if (key.Length == 0) return;
            
            EnsureCulture(key);

            if (CultureTexts.Any(x => x.CultureName.Equals(key, StringComparison.OrdinalIgnoreCase))) return;

            var vm = new TextsCultureViewModel()
            {
                CultureName = key
            };
            vm.New(null);
            CultureTexts.Add(vm);
            SelectedCultureText = vm;
            NotifyStatus("Culture added."); 
        }

        private void RemoveCulture()
        {
            if (SelectedCultureText == null) return;
            if (SelectedCultureText.CultureName.Equals(DefaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                NotifyStatus("Can't remove DefaultCulture."); 
                return;
            }

            CultureTexts.Remove(SelectedCultureText);
            SelectedCultureText = CultureTexts.FirstOrDefault();
            NotifyStatus("Culture removed.");
        }

        private void RegenerateAll() 
            => RegenerateTexts(null);

        private void RegenerateTexts(string? culture)
        {
            var regenerateAll = culture == null;
            EnsureCulture(culture ?? DefaultCulture);

            // Ensure that all columns have text entries in all cultures
            var expectedTextKeys = GetExpectedTexts();

            // For each culture, ensure all expected text keys exist and remove any unknown keys
            foreach (var vm in CultureTexts)
            {
                if (regenerateAll || vm.CultureName.Equals(culture, StringComparison.OrdinalIgnoreCase))
                {
                    RegenerateTexts(expectedTextKeys, vm.CultureName);
                }
            }

            NotifyStatus("Regenerated missing text entries for columns.");
        }

        private Dictionary<string, string> GetExpectedTexts()
        {
            if (Mode == TextsEditorMode.Preset)
            {
                return new Dictionary<string, string>()
                {
                    { KnownTextKeys.PresetTitle, "New preset" }
                };
            }
            else
            {
                var msg = SendMessage<GetColumnsMessage>();

                var expectedTextKeys = new Dictionary<string, string>()
                {
                    { KnownTextKeys.ReportTitle, "New report" }
                };

                foreach (var col in msg.Columns)
                {
                    expectedTextKeys[KnownTextKeys.GetColumnHeaderKey(col.Key)] = Humanize(col.Key);
                }

                return expectedTextKeys;
            }
        }

        private void RegenerateTexts(Dictionary<string, string> expectedTexts, string culture)
        {
            // Remove unknown keys
            var vm = EnsureCulture(culture);
            foreach (var textKey in vm.Texts.ToList())
            {
                if (string.IsNullOrEmpty(textKey.Key) || !expectedTexts.ContainsKey(textKey.Key!))
                {
                    vm.Texts.Remove(textKey);
                }
            }

            // Add missing keys
            foreach (var kv in expectedTexts)
            {
                var textVM = vm.Texts.FirstOrDefault(x => x.Key?.Equals(kv.Key, StringComparison.OrdinalIgnoreCase) == true);
                if (textVM == null)
                {
                    vm.Texts.Add(new TextEntryViewModel() { Key = kv.Key, Value = kv.Value });
                }
            }
        }

        private string ResolveText(string textKey, string culture)
        {
            var textsDict = new Dictionary<string, Dictionary<string, string>>();
            GetData(textsDict);

            return TextsResolver.ResolveText(textsDict, textKey, culture, DefaultCulture);
        }

        private static string Humanize(string key)
        {
            if (key.Contains('_'))
            {
                var parts = key.Split(["_"], StringSplitOptions.RemoveEmptyEntries);
                return string.Join(" ", parts.Select(ToTitle));
            }
            var s = Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            return ToTitle(s);
        }

        private static string ToTitle(string s) => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

        private void OnDefaultCultureChanged(string defaultCulture)
        {
            RegenerateTexts(defaultCulture);
        }

        private void OnColumnChangedMessageReceived(ColumnChangedMessage message)
        {
            if (Mode != TextsEditorMode.Report)
                return;

            string? oldKey = null;
            string? newKey = null;

            switch (message.ChangeKind)
            {
                case ColumnChangeKind.Added:
                    newKey = message.Column.Key;
                    break;
                case ColumnChangeKind.Deleted:
                    oldKey = message.Column.Key;
                    break;
                case ColumnChangeKind.Changed:
                    oldKey = message.PropertyValue!.OldValue as string;
                    newKey = message.PropertyValue!.NewValue as string;
                    break;
                default:
                    break;
            }

            var oldColumnKey = oldKey != null ? KnownTextKeys.GetColumnHeaderKey(oldKey) : null;
            var newColumnKey = newKey != null ? KnownTextKeys.GetColumnHeaderKey(newKey) : null;

            foreach (var vm in CultureTexts)
            {
                var textVM = oldColumnKey == null ? null : vm.Texts.FirstOrDefault(x => x.Key?.Equals(oldColumnKey, StringComparison.OrdinalIgnoreCase) == true);
                if (textVM != null)
                {
                    if (newColumnKey == null)
                    {
                        vm.Texts.Remove(textVM);
                    }
                    else
                    {
                        // if old name exists, rename it but keep the value
                        textVM.Key = newColumnKey;
                    }
                }
                else if (newColumnKey != null)
                {
                    var existingNewKeyTextVM = vm.Texts.FirstOrDefault(x => x.Key?.Equals(newColumnKey, StringComparison.OrdinalIgnoreCase) == true);
                    if (existingNewKeyTextVM == null)
                    {
                        textVM = new TextEntryViewModel()
                        {
                            Key = newColumnKey,
                            Value = Humanize(newKey!)
                        };
                        vm.Texts.Add(textVM);
                    }
                }
            }
        }

        private void OnCultureChangedMessageReceived(CultureChangedMessage message)
        {
            DefaultCulture = message.NewCulture;
            EnsureCulture(DefaultCulture);
        }

        private void OnResolveTextKeyMessageReceived(ResolveTextKeyMessage message)
        {
            if (message.TextsEditorMode != Mode)
            {
                return;
            }

            message.Value = ResolveText(message.Key, message.Culture);
        }
    }

    public enum TextsEditorMode
    {
        Report,
        Preset
    }
}
