using ReportManager.Lib.Wpf;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ReportManager.Client.ViewModels
{
    public sealed class ColumnPickerDialogViewModel : NotificationObject
    {
        #region Private Types

        private sealed class ColumnDescriptor
        {
            public required ColumnOption Column { get; init; }
            public required List<string> CategoryPathSegments { get; init; }
            public required string SearchText { get; init; }
        }

        private sealed class CategoryBuildNode
        {
            public CategoryBuildNode(string displayText)
            {
                DisplayText = displayText;
            }

            public string DisplayText { get; }
            public Dictionary<string, CategoryBuildNode> Children { get; } = new(StringComparer.OrdinalIgnoreCase);
            public List<ColumnOption> Columns { get; } = [];
        }

        #endregion

        #region Private Fields

        private readonly List<ColumnDescriptor> _allColumns;
        private readonly string? _initialSelectedColumnKey;

        #endregion

        #region Ctor

        public ColumnPickerDialogViewModel(IEnumerable<ColumnOption> availableColumns, ColumnOption? selectedColumn)
        {
            _allColumns = availableColumns
                .Select(column =>
                {
                    var categoryPathSegments = NormalizeCategoryPath(column);
                    var searchParts = new List<string>(categoryPathSegments) { column.DisplayName };
                    return new ColumnDescriptor
                    {
                        Column = column,
                        CategoryPathSegments = categoryPathSegments,
                        SearchText = string.Join(" ", searchParts)
                    };
                })
                .ToList();

            _initialSelectedColumnKey = selectedColumn?.Key;
            RebuildTree();
        }

        #endregion

        #region Properties

        public ObservableCollection<ColumnPickerNodeViewModel> RootNodes { get; } = [];

        public string FilterText { get; set => SetValue(ref field, value, _ => RebuildTree()); } = string.Empty;

        public ColumnPickerNodeViewModel? SelectedNode { get; set => SetValue(ref field, value, OnSelectedNodeChanged); }

        public ColumnOption? SelectedColumn { get; private set => SetValue(ref field, value, _ => OnPropertyChanged(nameof(CanConfirm))); }

        public bool CanConfirm => SelectedColumn != null;

        #endregion

        #region Private Methods

        private void RebuildTree()
        {
            var selectedKey = SelectedColumn?.Key ?? _initialSelectedColumnKey;
            var filter = FilterText?.Trim() ?? string.Empty;
            var hasFilter = filter.Length > 0;

            var sourceColumns = hasFilter
                ? _allColumns.Where(c => c.SearchText.Contains(filter, StringComparison.CurrentCultureIgnoreCase)).ToList()
                : _allColumns;

            var rootNode = new CategoryBuildNode(string.Empty);
            foreach (var columnDescriptor in sourceColumns)
            {
                var node = rootNode;
                foreach (var segment in columnDescriptor.CategoryPathSegments)
                {
                    if (!node.Children.TryGetValue(segment, out var childNode))
                    {
                        childNode = new CategoryBuildNode(segment);
                        node.Children[segment] = childNode;
                    }

                    node = childNode;
                }

                node.Columns.Add(columnDescriptor.Column);
            }

            RootNodes.Clear();
            foreach (var node in BuildUiNodes(rootNode, hasFilter))
            {
                RootNodes.Add(node);
            }

            SelectedNode = FindNodeByColumnKey(RootNodes, selectedKey);
            if (SelectedNode == null)
            {
                SelectedColumn = null;
            }
            else
            {
                SelectedNode.IsSelected = true;
            }
        }

        private static IEnumerable<ColumnPickerNodeViewModel> BuildUiNodes(CategoryBuildNode buildNode, bool expandCategories)
        {
            foreach (var childNode in buildNode.Children.Values.OrderBy(x => x.DisplayText, StringComparer.CurrentCultureIgnoreCase))
            {
                var uiCategoryNode = new ColumnPickerNodeViewModel
                {
                    DisplayText = childNode.DisplayText,
                    IsSelectable = false,
                    Column = null,
                    IsExpanded = expandCategories,
                    IsSelected = false
                };

                foreach (var childUiNode in BuildUiNodes(childNode, expandCategories))
                {
                    uiCategoryNode.Children.Add(childUiNode);
                }

                yield return uiCategoryNode;
            }

            foreach (var column in buildNode.Columns.OrderBy(x => x.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                yield return new ColumnPickerNodeViewModel
                {
                    DisplayText = column.DisplayName,
                    IsSelectable = true,
                    Column = column,
                    IsExpanded = false,
                    IsSelected = false
                };
            }
        }

        private static ColumnPickerNodeViewModel? FindNodeByColumnKey(IEnumerable<ColumnPickerNodeViewModel> nodes, string? columnKey)
        {
            if (string.IsNullOrWhiteSpace(columnKey))
            {
                return null;
            }

            foreach (var node in nodes)
            {
                if (node.Column != null && node.Column.Key.Equals(columnKey, StringComparison.OrdinalIgnoreCase))
                {
                    return node;
                }

                var childMatch = FindNodeByColumnKey(node.Children, columnKey);
                if (childMatch != null)
                {
                    node.IsExpanded = true;
                    return childMatch;
                }
            }

            return null;
        }

        private void OnSelectedNodeChanged(ColumnPickerNodeViewModel? selectedNode)
        {
            SelectedColumn = selectedNode?.IsSelectable == true ? selectedNode.Column : null;
        }

        private static List<string> NormalizeCategoryPath(ColumnOption column)
        {
            return (column.CategoryPath ?? [])
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x.Split(['/'], StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .ToList();
        }

        #endregion
    }

    public sealed class ColumnPickerNodeViewModel : NotificationObject
    {
        public required string DisplayText { get; init; }
        public required bool IsSelectable { get; init; }
        public required ColumnOption? Column { get; init; }
        public ObservableCollection<ColumnPickerNodeViewModel> Children { get; } = [];
        public bool IsExpanded { get; set => SetValue(ref field, value); }
        public bool IsSelected { get; set => SetValue(ref field, value); }
    }
}
