
namespace ReportManager.DefinitionModel.Utils
{
    public static class TextsResolver
    {
        public static string ResolveText(Dictionary<string, Dictionary<string, string>> texts, string textKey, string culture, string defaultCulture)
        {
            if (texts == null || string.IsNullOrEmpty(textKey))
            {
                return string.Empty;
            }

            Dictionary<string, string>? dict;
            if (texts.TryGetValue(culture, out dict)
                && dict != null
                && dict.TryGetValue(textKey!, out var t)
                && !string.IsNullOrEmpty(t))
                return t;

            if (culture.Contains('-'))
            {
                // try neutral culture
                var neutralCulture = culture.Split('-')[0];
                if (texts.TryGetValue(neutralCulture, out dict)
                    && dict != null
                    && dict.TryGetValue(textKey!, out t)
                    && !string.IsNullOrEmpty(t))
                    return t;
            }

            if (texts.TryGetValue(defaultCulture, out dict)
                && dict != null
                && dict.TryGetValue(textKey!, out t)
                && !string.IsNullOrEmpty(t))
                return t;

            // fallback to any
            foreach (var kv in texts)
            {
                dict = kv.Value;
                if (dict != null && dict.TryGetValue(textKey!, out t) && !string.IsNullOrEmpty(t))
                    return t;
            }

            return textKey;
        }

        public static void SetText(Dictionary<string, Dictionary<string, string>> texts, string presetTitle, string defaultCulture, string name)
        {
            if (texts == null)
            {
                return;
            }

            if (!texts.ContainsKey(defaultCulture))
            {
                texts[defaultCulture] = new Dictionary<string, string>();
            }

            texts[defaultCulture][presetTitle] = name;
        }
    }
}
