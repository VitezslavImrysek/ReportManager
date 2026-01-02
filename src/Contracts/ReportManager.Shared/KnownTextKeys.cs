namespace ReportManager.Shared
{
    public static class KnownTextKeys
    {
        public const string ReportTitle = "report.title";

        public static string GetColumnHeaderKey(string columnKey)
        {
            return $"col.{columnKey}";
        }
    }
}
