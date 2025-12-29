using ReportManager.Shared.Dto;
using System.Windows;
using System.Windows.Controls;

namespace ReportManager.Client.Behaviors
{
    public class LookupMultiSelectBehavior : Freezable
    {
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
                new PropertyMetadata(string.Empty));

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
            lb.SelectionChanged += Lb_SelectionChanged;
        }

        private void Detach(ListBox lb)
        {
            lb.SelectionChanged -= Lb_SelectionChanged;
        }

        private static void Lb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lb = (ListBox)sender;
            if (!lb.IsVisible)
            {
                return;
            }

            var keys = lb.SelectedItems
                .OfType<LookupItemDto>()
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // naplníme Value1 jako "A,B,C" -> GetValuesForDto už to umí rozparsovat
            var behavior = GetLookupMultiSelector(lb);
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
    }
}
