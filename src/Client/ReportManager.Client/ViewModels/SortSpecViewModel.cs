using ReportManager.Client.Views;
using ReportManager.Lib.Wpf;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace ReportManager.Client.ViewModels
{
    public sealed class SortSpecViewModel : NotificationObject
    {
        #region Ctor

        public SortSpecViewModel()
        {
            SelectColumnCommand = new RelayCommand(OpenColumnPicker);
        }

        #endregion

        #region Computed Properties

        public string SelectedColumnLabel
        {
            get
            {
                if (SelectedColumn == null)
                {
                    return "(No column selected)";
                }

                var categoryPath = (SelectedColumn.CategoryPath ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .SelectMany(x => x.Split(['/'], StringSplitOptions.RemoveEmptyEntries))
                    .Select(x => x.Trim())
                    .Where(x => x.Length > 0)
                    .ToList();

                if (categoryPath.Count == 0)
                {
                    return SelectedColumn.DisplayName;
                }

                return string.Join(" / ", categoryPath) + " / " + SelectedColumn.DisplayName;
            }
        }

        #endregion

        #region Properties

        public required ObservableCollection<ColumnOption> AvailableColumns { get; set => SetValue(ref field, value); }
        public ObservableCollection<SortDirection> Directions { get; } = [SortDirection.Asc, SortDirection.Desc];
        public ColumnOption? SelectedColumn { get; set => SetValue(ref field, value, OnSelectedColumnChanged); }
        public SortDirection SelectedDirection { get; set => SetValue(ref field, value); }

        public ICommand SelectColumnCommand { get; }
        public ICommand? RemoveCommand { get; set; }

        #endregion

        #region Public Methods

        public void SelectColumn(ColumnOption? column)
        {
            SelectedColumn = column;
        }

        #endregion

        #region Private Methods

        private void OpenColumnPicker()
        {
            if (AvailableColumns.Count == 0)
            {
                return;
            }

            var dialogVm = new ColumnPickerDialogViewModel(AvailableColumns, SelectedColumn);
            var dialog = new ColumnPickerDialog
            {
                Owner = Application.Current?.MainWindow,
                DataContext = dialogVm
            };

            if (dialog.ShowDialog() == true && dialogVm.SelectedColumn != null)
            {
                SelectColumn(dialogVm.SelectedColumn);
            }
        }

        private void OnSelectedColumnChanged(ColumnOption? selectedColumn)
        {
            OnPropertyChanged(nameof(SelectedColumnLabel));
        }

        #endregion
    }
}
