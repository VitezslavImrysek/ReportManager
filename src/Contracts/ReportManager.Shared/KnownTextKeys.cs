using System.Collections.Generic;
using System.Linq;

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

        public static string GetColumnCategoryPathKey(IEnumerable<string> categoryPath)
        {
            var normalized = categoryPath
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList();

            return $"colcat.{string.Join("/", normalized)}";
        }
    }
}
