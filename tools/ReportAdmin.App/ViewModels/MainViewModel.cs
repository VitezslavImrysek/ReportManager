using Microsoft.Win32;
using ReportAdmin.App.Dialogs;
using ReportAdmin.App.Extensions;
using ReportAdmin.App.Messages;
using ReportAdmin.Core;
using ReportAdmin.Core.Db;
using ReportAdmin.Core.Models;
using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Sql;
using ReportAdmin.Core.Utils;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace ReportAdmin.App.ViewModels;

public sealed class MainViewModel : NotificationObject
{
    #region Ctor

    public MainViewModel()
    {
        OpenFolderCommand = new RelayCommand(OpenFolder);
        NewReportCommand = new RelayCommand(NewReport);
        SaveGenerateCommand = new RelayCommand(SaveGenerate);
        ApplyToDbCommand = new RelayCommand(ApplyToDb);
        AddColumnCommand = new RelayCommand(AddColumn);
        RemoveSelectedColumnCommand = new RelayCommand(RemoveSelectedColumn);

		ReportHeaderVM = new ReportHeaderViewModel() { ImportColumnsCommand = new RelayCommand(ImportColumnsFromDb) };
        ReportTextsEditorVM = new TextsEditorViewModel() { Mode = TextsEditorMode.Report };

        Messenger.Instance.Register<GetColumnsMessage>(OnGetColumnsMessageReceived);
		Messenger.Instance.Register<GetCultureMessage>(OnGetCultureMessageReceived);
        Messenger.Instance.Register<GetReportKeyMessage>(OnGetReportKeyMessageReceived);

        var defaultReports = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        if (Directory.Exists(defaultReports))
            LoadFolder(defaultReports);
    }

    #endregion

    #region Properties

    public string RepoPath { get; set => SetValue(ref field, value); } = "(no folder)";

    public ObservableCollection<ReportFileItem> ReportFiles { get; } = new();

	private ReportFileItem? _selectedFile;
	public ReportFileItem? SelectedFile
	{
		get => _selectedFile;
		set
		{
			if (SetValue(ref _selectedFile, value))
				if (value != null) LoadFile(value.FilePath);
		}
	}

	public ReportSqlDocumentUi? Current { get; set => SetValue(ref field, value); }
	public ObservableCollection<ReportColumnType> ColumnTypeValues { get; } = new(Enum.GetValues(typeof(ReportColumnType)).Cast<ReportColumnType>());
	public ObservableCollection<ReportColumnViewModel> Columns { get; set => SetValue(ref field, value); }
    public ReportColumnViewModel? SelectedColumn { get; set => SetValue(ref field, value); }
    
    public string GeneratedSql { get; set => SetValue(ref field, value); } = string.Empty;
	public string StatusText { get; set => SetValue(ref field, value); } = "Ready";
	public string ApplyConnectionString { get; set => SetValue(ref field, value); } = string.Empty;

    #endregion

    #region Commands

    public RelayCommand OpenFolderCommand { get; }
	public RelayCommand NewReportCommand { get; }
	public RelayCommand SaveGenerateCommand { get; }
	public RelayCommand ApplyToDbCommand { get; }
	public RelayCommand AddColumnCommand { get; }
	public RelayCommand RemoveSelectedColumnCommand { get; }

    #endregion

    #region ViewModels

    public SystemPresetsEditorViewModel SystemPresetsEditorVM { get; } = new();
    public TextsEditorViewModel ReportTextsEditorVM { get; set => SetValue(ref field, value); }
    public ReportHeaderViewModel ReportHeaderVM { get; set => SetValue(ref field, value); }

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
			string folderPath = Path.GetDirectoryName(dialog.FileName);
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
			ReportFiles.Add(new ReportFileItem { FilePath = f });

		StatusText = $"Loaded folder: {folder} ({ReportFiles.Count} files)";
	}

	private void LoadFile(string path)
	{
		try
		{
			Current = ReportSqlParser.LoadFromFile(path);
			if (Current.Definition == null)
			{
                // Contains no or invalid definition.
				MessageBox.Show("The report SQL file does not contain a valid report definition.");
				return;
            }

			ReportHeaderVM.SetData(Current);

            ReportTextsEditorVM.DefaultCulture = Current.Definition.DefaultCulture;
			ReportTextsEditorVM.SetData(Current.Definition.Texts);

			SystemPresetsEditorVM.SetData(Current.SystemPresets);
			// map selected column to UI model
			Columns = Current.Definition.Columns.Select(x => {
				var vm = new ReportColumnViewModel()
				{
					ColumnTypeValues = ColumnTypeValues
                };
				vm.SetData(x);
				return vm;
            }).ToObservable();
            SelectedColumn = Columns.FirstOrDefault();

			GeneratedSql = ReportSqlGenerator.GenerateSql(Current);
			StatusText = $"Loaded: {Path.GetFileName(path)}";
		}
		catch (Exception ex)
		{
			StatusText = "Load error: " + ex.Message;
		}
	}

	private void NewReport()
	{
		Current = new ReportSqlDocumentUi
		{
			ReportKey = "NewReport",
			ViewSchema = "dbo",
			ViewName = "v_YourView",
			Definition = new ReportDefinitionUi 
			{ 
				DefaultCulture = Constants.DefaultLanguage, 
				Columns = [],
				DefaultSort = [],
				Texts = []
			},
			SystemPresets = []
		};

		Current.Definition.Texts[Constants.DefaultLanguage] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["report.title"] = "New report"
        };

        ReportHeaderVM.SetData(Current);
        ReportTextsEditorVM.SetData(Current.Definition.Texts);

		GeneratedSql = ReportSqlGenerator.GenerateSql(Current);
		StatusText = "New report created (not saved yet).";
	}

	private void SaveGenerate()
	{
		try
		{
			if (Current == null) return;

			Current.SystemPresets = [];
            SystemPresetsEditorVM.GetData(Current.SystemPresets);

			foreach (var p in Current.SystemPresets)
			{
				if (string.IsNullOrWhiteSpace(p.PresetKey))
					throw new InvalidOperationException("PresetKey cannot be empty.");
				p.PresetId = GuidUtil.FromPresetKey(p.PresetKey);
			}

            ValidateReportDefinition();
            ValidateLookupSqls();

			var report = GenerateReport();

			GeneratedSql = ReportSqlGenerator.GenerateSql(report);

			if (string.IsNullOrWhiteSpace(RepoPath) || RepoPath == "(no folder)")
				throw new InvalidOperationException("Open folder first.");

			var file = Path.Combine(RepoPath, Current.ReportKey + ".sql");
			File.WriteAllText(file, GeneratedSql);

			LoadFolder(RepoPath);
			SelectedFile = ReportFiles.FirstOrDefault(x => x.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase));

			StatusText = $"Saved: {file}";
		}
		catch (Exception ex)
		{
			StatusText = "Save error: " + ex.Message;
		}
	}

    private async void ApplyToDb()
	{
		if (Current == null) return;

		try
		{
			if (string.IsNullOrWhiteSpace(ApplyConnectionString))
				throw new InvalidOperationException("Connection string is empty.");

            Current.SystemPresets = [];
            SystemPresetsEditorVM.GetData(Current.SystemPresets);

            ValidateReportDefinition();
			ValidateLookupSqls();

            var report = GenerateReport();

            GeneratedSql = ReportSqlGenerator.GenerateSql(report);

			StatusText = "Applying to DB...";
			await SqlBatchExecutor.ExecuteScriptAsync(ApplyConnectionString, GeneratedSql);
			StatusText = "Apply complete.";
		}
		catch (Exception ex)
		{
			StatusText = "Apply error: " + ex.Message;
		}
	}

    private ReportSqlDocumentUi GenerateReport()
    {
        var report = Current;
        report.Definition.Columns = Columns.Select(vm =>
        {
            var ui = new ReportColumnUi();
            vm.GetData(ui);
            return ui;
        }).ToObservable();
        ReportHeaderVM.GetData(report);

		report.Definition.Texts = new Dictionary<string, Dictionary<string, string>>();
        ReportTextsEditorVM.GetData(report.Definition.Texts);

		report.SystemPresets = new ObservableCollection<Core.Models.Preset.SystemPresetUi>();
        SystemPresetsEditorVM.GetData(report.SystemPresets);

        return report;
    }

    private async void ImportColumnsFromDb()
	{
		if (Current?.Definition == null) return;

		try
		{
			var dlgVM = new ImportDialogViewModel
			{
				ConnStringText = ApplyConnectionString,
				SchemaText = Current.ViewSchema,
				ViewText = Current.ViewName
			};

			var dlg = new ImportDialog { DataContext = dlgVM, Owner = Application.Current.MainWindow };
			if (dlg.ShowDialog() != true) return;

			ApplyConnectionString = dlgVM.ConnStringText;
			Current.ViewSchema = dlgVM.SchemaText;
			Current.ViewName = dlgVM.ViewText;

			StatusText = "Reading view metadata...";
			var cols = await DbIntrospector.GetViewColumnsAsync(dlgVM.ConnStringText, dlgVM.SchemaText, dlgVM.ViewText);

            // Delete non-virtual columns which are not present in the imported view
			var existingColumnKeys = cols.Select(c => c.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
				if (col.Virtual)
				{
					continue;
				}

				if (!existingColumnKeys.Contains(col.Key))
				{
                    Columns.RemoveAt(i);
                    i--;
                    Messenger.Instance.Send(new ReportColumnKeyChangedMessage() { OldName = col.Key });
                }
            }

            // Add or update imported columns
            foreach (var col in cols)
			{
				var oldColumn = Columns.FirstOrDefault(c => string.Equals(c.Key, col.Name, StringComparison.OrdinalIgnoreCase));
				if (oldColumn != null)
				{
                    // Update type if changed
                    var newType = DbIntrospector.MapSqlType(col.SqlType);
                    if (oldColumn.Type != newType)
                        oldColumn.Type = newType;
                }
				else
				{
                    var type = DbIntrospector.MapSqlType(col.SqlType);
                    var textKey = KnownTextKeys.GetColumnHeaderKey(col.Name);

					var ui = new ReportColumnUi
					{
						Key = col.Name,
						Type = type,
					};

                    var vm = new ReportColumnViewModel()
					{
						 ColumnTypeValues = ColumnTypeValues
                    };
					vm.SetData(ui);

                    Columns.Add(vm);

                    Messenger.Instance.Send(new ReportColumnKeyChangedMessage() { NewName = col.Name });
                }
            }

			SelectedColumn = Columns.FirstOrDefault();
			GeneratedSql = ReportSqlGenerator.GenerateSql(Current);
			StatusText = $"Imported {cols.Count} columns from {dlgVM.SchemaText}.{dlgVM.ViewText}.";
		}
		catch (Exception ex)
		{
			StatusText = "Import error: " + ex.Message;
		}
	}

	private void AddColumn()
	{
		if (Current?.Definition == null) return;

		var ui = new ReportColumnUi
		{
			Key = "new_column",
			Type = ReportColumnType.String,
		};

		var vm = new ReportColumnViewModel()
		{
			ColumnTypeValues = ColumnTypeValues
        };
		vm.SetData(ui);
		Columns.Add(vm);
		SelectedColumn = vm;
    }

	private void RemoveSelectedColumn()
	{
		if (Current?.Definition == null) return;
		if (SelectedColumn == null) return;
		// remove underlying json column by key
		Columns.Remove(SelectedColumn);
		SelectedColumn = Columns.FirstOrDefault();
		StatusText = "Column removed.";
	}

	private void ValidateLookupSqls()
	{
		if (Current?.Definition == null) return;

		var errors = new List<string>();
		foreach (var column in Columns)
		{
			var lookup = column.Filter?.Lookup;
			if (lookup?.Mode != LookupMode.Sql || lookup.Sql == null)
				continue;

			if (!SqlLookupValidator.TryValidate(lookup.Sql.CommandText, out var error))
				errors.Add($"{column.Key}: {error}");
		}

		if (errors.Count > 0)
			throw new InvalidOperationException("Lookup SQL validation failed:\n" + string.Join(Environment.NewLine, errors));
	}

    private void ValidateReportDefinition()
    {
        if (Current?.Definition == null) return;

        var errors = new List<string>();
        var definition = Current.Definition;
        var defaultCulture = definition.DefaultCulture;

        if (string.IsNullOrWhiteSpace(defaultCulture))
        {
            errors.Add("DefaultCulture is missing.");
        }
        else if (!definition.Texts.ContainsKey(defaultCulture))
        {
            errors.Add($"Missing texts for default culture '{defaultCulture}'.");
        }

        var expectedTextKeys = new Dictionary<string, string>()
        {
            { KnownTextKeys.ReportTitle, "New report" }
        };

        foreach (var col in Columns)
        {
            expectedTextKeys[KnownTextKeys.GetColumnHeaderKey(col.Key)] = Humanize(col.Key);
        }

        // For each culture, ensure all expected text keys exist and remove any unknown keys
        foreach (var culture in Current.Definition.Texts.Keys)
        {
            // Remove unknown keys
            var cultureTexts = Current.Definition.Texts[culture];
            foreach (var textKey in cultureTexts.Keys.ToList())
            {
                if (!expectedTextKeys.ContainsKey(textKey))
                {
                    errors.Add($"Unexpected text '{textKey}' in culture '{culture}'.");
                }
            }

            // Add missing keys
            foreach (var kv in expectedTextKeys)
            {
                if (!cultureTexts.ContainsKey(kv.Key))
                {
					errors.Add($"Missing text '{kv.Key}' in culture '{culture}'.");
                }
            }
        }

        if (errors.Count > 0)
            throw new InvalidOperationException("Report definition validation failed:\n" + string.Join(Environment.NewLine, errors));
    }

    private static string Humanize(string key)
	{
		if (key.Contains('_'))
		{
			var parts = key.Split(["_"], StringSplitOptions.RemoveEmptyEntries);
			return string.Join(" ", parts.Select(ToTitle));
		}
		var s = Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
		return ToTitle(s);
	}

	private static string ToTitle(string s) => string.IsNullOrWhiteSpace(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

    #endregion

    #region IMessageReceiver implementation

    private void OnGetColumnsMessageReceived(GetColumnsMessage message)
    {
        foreach (var col in Current?.Definition?.Columns ?? [])
        {
            message.Columns.Add(col);
        }
    }

    private void OnGetCultureMessageReceived(GetCultureMessage message)
    {
		message.Culture = Current.Definition.DefaultCulture;
    }

    private void OnGetReportKeyMessageReceived(GetReportKeyMessage message)
    {
		message.ReportKey = Current.ReportKey;
    }

    #endregion
}
