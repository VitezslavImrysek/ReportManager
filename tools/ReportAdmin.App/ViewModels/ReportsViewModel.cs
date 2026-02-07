using Microsoft.Win32;
using ReportAdmin.App.Messages;
using ReportAdmin.Core;
using ReportManager.Lib.Wpf;
using ReportManager.Lib.Wpf.Messages;
using System.Collections.ObjectModel;
using System.IO;

namespace ReportAdmin.App.ViewModels
{
    public class ReportsViewModel : NotificationObject
    {
        #region Ctor

        public ReportsViewModel()
        {
            ReportVM = new ReportViewModel();

            OpenFolderCommand = new RelayCommand(OpenFolder);
            NewReportCommand = new RelayCommand(NewReport);

            Messenger.Instance.Register<StatusMessage>(OnStatusMessageReceived);
            Messenger.Instance.Register<RefreshReportsMessage>(OnRefreshReportsMessageReceived);

            var defaultReports = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            if (Directory.Exists(defaultReports))
                LoadFolder(defaultReports);
        }

        #endregion

        #region Properties

        public string RepoPath { get; set => SetValue(ref field, value); } = "(no folder)";
        public string? StatusText { get; set => SetValue(ref field, value); } = "Ready";

        public ObservableCollection<ReportFileItem> ReportFiles { get; } = [];

        public ReportFileItem? SelectedFile { get; set => SetValue(ref field, value, OnSelectedFileChanged); }

        private void OnSelectedFileChanged(ReportFileItem? item)
        {
            if (item == null) return;

            ReportVM.SetData(item);
        }

        #endregion

        #region Commands

        public RelayCommand OpenFolderCommand { get; }
        public RelayCommand NewReportCommand { get; }

        #endregion

        #region ViewModels

        public ReportViewModel ReportVM { get; set => SetValue(ref field, value); }

        #endregion

        #region Private Methods

        private void OpenFolder()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select folder containing report SQL files.",
                Filter = "Folders|\n",
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "Vybrat složku",
                ValidateNames = false
            };

            if (dialog.ShowDialog() == true)
            {
                var folderPath = Path.GetDirectoryName(dialog.FileName);
                if (Directory.Exists(folderPath))
                {
                    LoadFolder(folderPath);
                }
            }
        }

        private void LoadFolder(string folder)
        {
            RepoPath = folder;

            ReportFiles.Clear();
            foreach (var f in Directory.GetFiles(folder, "*.sql").OrderBy(Path.GetFileName))
            {
                ReportFiles.Add(new ReportFileItem { FilePath = f });
            }

            StatusText = $"Loaded folder: {folder} ({ReportFiles.Count} files)";
        }

        private void NewReport()
        {
            ReportVM.New(new ReportContext() { ReportFolder = RepoPath });
        }

        private void OnStatusMessageReceived(StatusMessage message)
        {
            StatusText = message.Text;
        }

        private void OnRefreshReportsMessageReceived(RefreshReportsMessage message)
        {
            var selectedFile = SelectedFile;

            LoadFolder(RepoPath);

            if (selectedFile != null)
            {
                SelectedFile = ReportFiles.FirstOrDefault(x => x.FilePath.Equals(selectedFile.FilePath, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion
    }
}
