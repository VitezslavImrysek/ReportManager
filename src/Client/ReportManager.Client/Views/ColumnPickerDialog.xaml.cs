using ReportManager.Client.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ReportManager.Client.Views
{
    /// <summary>
    /// Interaction logic for ColumnPickerDialog.xaml
    /// </summary>
    public partial class ColumnPickerDialog : Window
    {
        public ColumnPickerDialog()
        {
            InitializeComponent();
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is ColumnPickerDialogViewModel vm)
            {
                vm.SelectedNode = e.NewValue as ColumnPickerNodeViewModel;
            }
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void SelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (DataContext is ColumnPickerDialogViewModel vm && vm.CanConfirm)
            {
                DialogResult = true;
            }
        }
    }
}
