using ReportManager.Shared.Dto;
using System.Windows;
using System.Windows.Controls;

namespace ReportManager.Client.Behaviors
{
    public class LookupMultiSelectBehavior : Freezable
    {
        private bool _isSyncing;
        private ListBox? _listBox;

        public static readonly DependencyProperty LookupMultiSelectorProperty =
            DependencyProperty.RegisterAttached(
                "LookupMultiSelector",
                typeof(LookupMultiSelectBehavior),
                typeof(LookupMultiSelectBehavior),
                new PropertyMetadata(null, new PropertyChangedCallback(OnLookupMultiSelectorChanged)));

        public static readonly DependencyProperty BoundTextProperty =
            DependencyProperty.Register(
                "BoundText",
                typeof(string),
                typeof(LookupMultiSelectBehavior),
                new PropertyMetadata(string.Empty, OnBoundTextChanged));

        public string BoundText
        {
            get { return (string)GetValue(BoundTextProperty); }
            set { SetValue(BoundTextProperty, value); }
        }

        public static LookupMultiSelectBehavior GetLookupMultiSelector(ListBox obj)
        {
            return (LookupMultiSelectBehavior)obj.GetValue(LookupMultiSelectorProperty);
        }

        public static void SetLookupMultiSelector(ListBox obj, LookupMultiSelectBehavior value)
        {
            obj.SetValue(LookupMultiSelectorProperty, value);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LookupMultiSelectBehavior();
        }

        private void Attach(ListBox lb)
        {
            _listBox = lb;
            lb.SelectionChanged += Lb_SelectionChanged;
            SyncSelectionFromText();
        }

        private void Detach(ListBox lb)
        {
            lb.SelectionChanged -= Lb_SelectionChanged;
            if (_listBox == lb)
            {
                _listBox = null;
            }
        }

        private static void Lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox)sender;
            if (!lb.IsVisible)
            {
                return;
            }

            var behavior = GetLookupMultiSelector(lb);
            if (behavior._isSyncing)
            {
                return;
            }

            var keys = lb.SelectedItems
                .OfType<LookupItemDto>()
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // populate Value1 as "A,B,C" -> GetValuesForDto can already parse it
            behavior.SetCurrentValue(BoundTextProperty, string.Join(",", keys));
        }

        private static void OnLookupMultiSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var lb = d as ListBox;
            if (lb != null)
            {
                var oldBehavior = (LookupMultiSelectBehavior)e.OldValue;
                oldBehavior?.Detach(lb);

                var newBehavior = (LookupMultiSelectBehavior)e.NewValue;
                newBehavior?.Attach(lb);
            }
        }

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (LookupMultiSelectBehavior)d;
            behavior.SyncSelectionFromText();
        }

        private void SyncSelectionFromText()
        {
            if (_listBox == null)
            {
                return;
            }

            var raw = BoundText ?? string.Empty;
            var keys = raw
                .Split([',', ';', '\n', '\r', '\t', ' '], StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _isSyncing = true;
            try
            {
                _listBox.UnselectAll();
                if (keys.Count == 0)
                {
                    return;
                }

                foreach (var item in _listBox.Items.OfType<LookupItemDto>())
                {
                    if (item.Key != null && keys.Contains(item.Key))
                    {
                        _listBox.SelectedItems.Add(item);
                    }
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }
    }
}
