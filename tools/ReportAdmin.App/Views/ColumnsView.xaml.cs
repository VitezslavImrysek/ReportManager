using ReportAdmin.App.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace ReportAdmin.App.Views
{
    /// <summary>
    /// Interaction logic for ColumnsView.xaml
    /// </summary>
    public partial class ColumnsView : UserControl
    {
        private DataGridRow? _draggedRow;
        private int _draggedIndex = -1;
        private Point? _dragStartPoint;

        public ColumnsView()
        {
            InitializeComponent();
        }

        private void DataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not FrameworkElement element)
                return;

            if (FindVisualParent<TextBoxBase>(element) != null)
            {
                _draggedRow = null;
                _draggedIndex = -1;
                _dragStartPoint = null;
                return;
            }

            var row = FindVisualParent<DataGridRow>(element);
            if (row == null || row.IsEditing)
                return;

            _draggedRow = row;
            _draggedIndex = dataGrid.Items.IndexOf(row.Item);
            _dragStartPoint = e.GetPosition(null);
        }

        private void DataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                _dragStartPoint = null;
                return;
            }

            if (_draggedRow == null || _dragStartPoint == null)
                return;

            var currentPosition = e.GetPosition(null);
            if (Math.Abs(currentPosition.X - _dragStartPoint.Value.X) < SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(currentPosition.Y - _dragStartPoint.Value.Y) < SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            if (_draggedRow.IsEditing)
            {
                _dragStartPoint = null;
                return;
            }

            DragDrop.DoDragDrop(_draggedRow, _draggedRow.Item, DragDropEffects.Move);
            _dragStartPoint = null;
        }

        private void DataGrid_Drop(object sender, DragEventArgs e)
        {
            if (_draggedRow == null || DataContext is not ColumnsViewModel viewModel)
                return;

            var targetElement = e.OriginalSource as FrameworkElement;
            if (targetElement == null)
                return;

            var targetRow = FindVisualParent<DataGridRow>(targetElement);
            if (targetRow == null || targetRow == _draggedRow)
                return;

            var targetIndex = dataGrid.Items.IndexOf(targetRow.Item);

            if (_draggedIndex >= 0 && targetIndex >= 0 && _draggedIndex != targetIndex)
            {
                viewModel.Columns.Move(_draggedIndex, targetIndex);
                viewModel.MoveUpCommand?.RaiseCanExecuteChanged();
                viewModel.MoveDownCommand?.RaiseCanExecuteChanged();
            }

            _draggedRow = null;
            _draggedIndex = -1;
            _dragStartPoint = null;
        }

        private void DataGrid_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parentObject = child;
            while (parentObject != null)
            {
                if (parentObject is T parent)
                    return parent;
                parentObject = System.Windows.Media.VisualTreeHelper.GetParent(parentObject);
            }
            return null;
        }
    }
}
