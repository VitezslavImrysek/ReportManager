namespace ReportManager.Shared
{
    public static class KnownTextKeys
    {
        public const string ReportTitle = "report.title";
        public const string PresetTitle = "preset.title";

        public static string GetColumnHeaderKey(string columnKey)
        {
            return $"col.{columnKey}";
        }

        public static string GetColumnCategoryKey(string categoryKey)
        {
            return $"colcat.{categoryKey?.Trim() ?? string.Empty}";
        }
    }
}
