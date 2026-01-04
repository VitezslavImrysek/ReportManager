using ReportAdmin.App.Messages;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Shared;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ReportAdmin.App.ViewModels
{
    public class TextsEditorContext
    {

    }

    public class TextsEditorViewModel : DataEditorVM<Dictionary<string, Dictionary<string, string>>, TextsEditorContext>, 
        IMessageReceiver<ReportColumnKeyChangedMessage>,
        IMessageReceiver<DefaultCultureChangedMessage>
    {
        private Dictionary<string, Dictionary<string, string>>? _texts;

        public TextsEditorViewModel()
        {
            Messenger.Instance.Register<ReportColumnKeyChangedMessage>(this);
            Messenger.Instance.Register<DefaultCultureChangedMessage>(this);

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
                CommitCultureEntries();
                var textKey = Mode == TextsEditorMode.Report ? KnownTextKeys.ReportTitle : KnownTextKeys.PresetTitle;
                return TextsResolver.ResolveText(_texts, textKey, DefaultCulture, DefaultCulture);
            }
        }

        private void OnDefaultCultureChanged(string defaultCulture)
        {
            RegenerateTexts(defaultCulture);
        }

        public ObservableCollection<string> CultureKeys { get; } = new();

        public string? SelectedCultureKey
        {
            get;
            set
            {
                if (SetValue(ref field, value))
                    LoadCultureEntries();
            }
        }

        public ObservableCollection<KvRowVm> CultureEntries { get; } = [];
        public string SelectedCultureTitle => SelectedCultureKey == null ? "No culture selected" : $"Culture: {SelectedCultureKey}";

        protected override void OnNew(TextsEditorContext context)
        {
            
        }

        protected override void OnSetData(Dictionary<string, Dictionary<string, string>> data)
        {
            _texts = data;

            CultureKeys.Clear();
            foreach (var c in data.Keys.OrderBy(x => x))
                CultureKeys.Add(c);

            SelectedCultureKey = CultureKeys.FirstOrDefault();
        }

        protected override Dictionary<string, Dictionary<string, string>> OnGetData()
        {
            CommitCultureEntries();
            return _texts;
        }

        private void LoadCultureEntries()
        {
            if (_texts == null) return;
            CultureEntries.Clear();
            if (SelectedCultureKey == null) return;

            var cultureTexts = EnsureCulture(SelectedCultureKey);

            foreach (var kv in cultureTexts.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                CultureEntries.Add(new KvRowVm { Key = kv.Key, Value = kv.Value });

            OnPropertyChanged(nameof(SelectedCultureTitle));
        }

        private Dictionary<string, string> EnsureCulture(string key)
        {
            if (_texts == null) return [];

            if (!_texts.TryGetValue(key, out var cultureTexts))
            {
                cultureTexts = _texts[key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return cultureTexts;
        }

        private void CommitCultureEntries()
        {
            if (_texts == null) return;
            if (SelectedCultureKey == null) return;

            var cultureTexts = EnsureCulture(SelectedCultureKey);
            cultureTexts.Clear();

            foreach (var row in CultureEntries)
            {
                var k = (row.Key ?? string.Empty).Trim();
                if (k.Length == 0) continue;
                cultureTexts[k] = row.Value ?? string.Empty;
            }
        }

        private void AddCulture()
        {
            var key = Microsoft.VisualBasic.Interaction.InputBox("Culture key (e.g. cs, en, pl):", "Add culture", "en").Trim();
            if (key.Length == 0) return;
            EnsureCulture(key);
            if (!CultureKeys.Contains(key)) CultureKeys.Add(key);
            SelectedCultureKey = key;
            NotifyStatus("Culture added."); ;
        }

        private void RemoveCulture()
        {
            if (_texts == null) return;
            if (SelectedCultureKey == null) return;
            if (SelectedCultureKey.Equals(DefaultCulture, StringComparison.OrdinalIgnoreCase))
            {
                NotifyStatus("Can't remove DefaultCulture."); 
                return;
            }

            _texts.Remove(SelectedCultureKey);
            CultureKeys.Remove(SelectedCultureKey);
            SelectedCultureKey = CultureKeys.FirstOrDefault();
            NotifyStatus("Culture removed.");
        }

        private void RegenerateAll() 
            => RegenerateTexts(null);

        private void RegenerateTexts(string? culture)
        {
            if (_texts == null) return;
            CommitCultureEntries();

            var regenerateAll = culture == null;
            EnsureCulture(culture ?? DefaultCulture);

            // Ensure that all columns have text entries in all cultures
            var expectedTextKeys = GetExpectedTexts();

            // For each culture, ensure all expected text keys exist and remove any unknown keys
            foreach (var cultureKey in _texts.Keys)
            {
                if (regenerateAll || cultureKey.Equals(culture, StringComparison.OrdinalIgnoreCase))
                {
                    RegenerateTexts(expectedTextKeys, cultureKey);
                }
            }

            LoadCultureEntries();
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
                var msg = Messenger.Instance.Send<GetColumnsMessage>();

                var expectedTextKeys = new Dictionary<string, string>()
                {
                    { KnownTextKeys.ReportTitle, "New report" }
                };

                foreach (var col in msg.ColumnNames)
                {
                    expectedTextKeys[KnownTextKeys.GetColumnHeaderKey(col)] = Humanize(col);
                }

                return expectedTextKeys;
            }
        }

        private void RegenerateTexts(Dictionary<string, string> expectedTexts, string culture)
        {
            // Remove unknown keys
            var cultureTexts = EnsureCulture(culture);
            foreach (var textKey in cultureTexts.Keys.ToList())
            {
                if (!expectedTexts.ContainsKey(textKey))
                {
                    cultureTexts.Remove(textKey);
                }
            }

            // Add missing keys
            foreach (var kv in expectedTexts)
            {
                if (!cultureTexts.ContainsKey(kv.Key))
                {
                    cultureTexts[kv.Key] = kv.Value;
                }
            }
        }

        private void NotifyStatus(string status)
        {
            Messenger.Instance.Send(new StatusMessage { Text = status });
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

        void IMessageReceiver<ReportColumnKeyChangedMessage>.Receive(ReportColumnKeyChangedMessage message)
        {
            if (Mode != TextsEditorMode.Report)
                return;

            foreach (var culture in _texts!.Values)
            {
                var oldColumnKey = message.OldName != null ? KnownTextKeys.GetColumnHeaderKey(message.OldName) : null;
                var newColumnKey = message.NewName != null ? KnownTextKeys.GetColumnHeaderKey(message.NewName) : null;

                if (oldColumnKey != null && culture.ContainsKey(oldColumnKey))
                {
                    // if old name exists, rename it but keep the value
                    var value = culture[oldColumnKey];
                    culture.Remove(oldColumnKey);
                    if (newColumnKey != null)
                        culture[newColumnKey] = value;
                }
                else if (newColumnKey != null && !culture.ContainsKey(newColumnKey))
                {
                    culture[newColumnKey] = Humanize(message.NewName);
                }
            }
        }

        void IMessageReceiver<DefaultCultureChangedMessage>.Receive(DefaultCultureChangedMessage message)
        {
            DefaultCulture = message.NewDefaultCulture;
            EnsureCulture(DefaultCulture);
        }
    }

    public enum TextsEditorMode
    {
        Report,
        Preset
    }
}
