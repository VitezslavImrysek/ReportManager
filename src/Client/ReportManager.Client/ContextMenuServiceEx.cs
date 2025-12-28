using System.Windows;
using System.Windows.Controls.Primitives;

namespace ReportManager.Client
{
    public static class ContextMenuServiceEx
    {
        public static readonly DependencyProperty OpenOnLeftClickProperty =
            DependencyProperty.RegisterAttached(
                "OpenOnLeftClick",
                typeof(bool),
                typeof(ContextMenuServiceEx),
                new PropertyMetadata(false, OnOpenOnLeftClickChanged));

        public static void SetOpenOnLeftClick(DependencyObject element, bool value)
            => element.SetValue(OpenOnLeftClickProperty, value);

        public static bool GetOpenOnLeftClick(DependencyObject element)
            => (bool)element.GetValue(OpenOnLeftClickProperty);

        private static void OnOpenOnLeftClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ButtonBase btn) return;

            if ((bool)e.NewValue)
                btn.Click += Btn_Click;
            else
                btn.Click -= Btn_Click;
        }

        private static void Btn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not ButtonBase btn) return;
            var menu = btn.ContextMenu;
            if (menu == null) return;

            menu.PlacementTarget = btn;
            menu.Placement = PlacementMode.Bottom;
            menu.IsOpen = true;
            e.Handled = true;
        }
    }
}
