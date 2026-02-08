using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace ReportManager.Client.ViewModels
{
    public sealed class ColumnPickerItem
    {
        public required string DisplayText { get; init; }
        public required int Level { get; init; }
        public ColumnOption? Column { get; init; }

        public bool IsCategory => Column == null;
        public bool IsSelectable => Column != null;
        public Thickness Indent => new Thickness(Level * 14, 0, 0, 0);
    }

    public static class ColumnPickerFactory
    {
        public static ObservableCollection<ColumnPickerItem> Build(IEnumerable<ColumnOption> columns)
        {
            var root = new CategoryNode(string.Empty);

            foreach (var column in columns)
            {
                var node = root;
                var categoryPath = column.CategoryPath ?? [];
                foreach (var segment in categoryPath.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
                {
                    if (!node.Children.TryGetValue(segment, out var child))
                    {
                        child = new CategoryNode(segment);
                        node.Children[segment] = child;
                    }

                    node = child;
                }

                node.Columns.Add(column);
            }

            var items = new List<ColumnPickerItem>();
            AddItems(root, 0, items);
            return new ObservableCollection<ColumnPickerItem>(items);
        }

        private static void AddItems(CategoryNode categoryNode, int level, List<ColumnPickerItem> items)
        {
            foreach (var child in categoryNode.Children.Values.OrderBy(x => x.DisplayText, StringComparer.CurrentCultureIgnoreCase))
            {
                items.Add(new ColumnPickerItem
                {
                    DisplayText = child.DisplayText,
                    Level = level,
                    Column = null
                });

                AddItems(child, level + 1, items);
            }

            foreach (var column in categoryNode.Columns.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                items.Add(new ColumnPickerItem
                {
                    DisplayText = column.DisplayName,
                    Level = level,
                    Column = column
                });
            }
        }

        private sealed class CategoryNode
        {
            public CategoryNode(string displayText)
            {
                DisplayText = displayText;
            }

            public string DisplayText { get; }
            public Dictionary<string, CategoryNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<ColumnOption> Columns { get; } = [];
        }
    }
}
