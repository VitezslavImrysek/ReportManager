using ReportManager.ApiContracts.Dto;
using System;
using System.Linq;
using System.Windows;

namespace ReportManager.Client
{
    public static class LookupMultiSelectBehavior
    {
        public static readonly DependencyProperty BoundTextProperty =
            DependencyProperty.RegisterAttached(
                "BoundText",
                typeof(string),
                typeof(LookupMultiSelectBehavior),
                new PropertyMetadata(string.Empty, OnBoundTextChanged));

        public static void SetBoundText(DependencyObject element, string value) =>
            element.SetValue(BoundTextProperty, value);

        public static string GetBoundText(DependencyObject element) =>
            (string)element.GetValue(BoundTextProperty);

        private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not System.Windows.Controls.ListBox lb)
                return;

            lb.SelectionChanged -= Lb_SelectionChanged;
            lb.SelectionChanged += Lb_SelectionChanged;
        }

        private static void Lb_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var lb = (System.Windows.Controls.ListBox)sender;
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
            lb.SetCurrentValue(BoundTextProperty, string.Join(",", keys));
        }
    }

}
