using Microsoft.Win32;
using ReportAdmin.App.Dialogs;
using ReportAdmin.Core;
using ReportAdmin.Core.Db;
using ReportAdmin.Core.Models;
using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Models.Preset;
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
	public PresetEditorViewModel PresetEditor { get; } = new();
	public ReportColumnViewModel ColumnEditor { get; } = new();

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

	private ReportSqlDocumentUi? _current;
	public ReportSqlDocumentUi? Current
	{
		get => _current;
		set
		{
			if (SetValue(ref _current, value))
			{
				OnPropertyChanged(nameof(CultureKeys));
			}
		}
	}

	public ObservableCollection<ReportColumnType> ColumnTypeValues { get; } = new(Enum.GetValues(typeof(ReportColumnType)).Cast<ReportColumnType>());

	public ReportColumnUi? SelectedColumn
	{
		get;
		set
		{
			if (SetValue(ref field, value))
			{
				ColumnEditor.Column = value;
				OnPropertyChanged(nameof(SelectedColumnHasLookup));
				OnPropertyChanged(nameof(SelectedLookupCommandText));
				OnPropertyChanged(nameof(SelectedLookupKeyColumn));
				OnPropertyChanged(nameof(SelectedLookupTextColumn));
			}
		}
	}

	public SystemPresetUi? SelectedPreset { get; set => SetValue(ref field, value); }
	public string GeneratedSql { get; set => SetValue(ref field, value); } = string.Empty;
	public string StatusText { get; set => SetValue(ref field, value); } = "Ready";
	public string ApplyConnectionString { get; set => SetValue(ref field, value); } = string.Empty;

    public ObservableCollection<string> CultureKeys { get; } = new();

	public string? SelectedCultureKey
	{
		get;
		set
		{
			if (SetValue(ref field, value))
				LoadCultureEntries();
		}
	}
	public string SelectedCultureTitle => SelectedCultureKey == null ? "No culture selected" : $"Culture: {SelectedCultureKey}";
	public ObservableCollection<KvRowVm> CultureEntries { get; } = [];

	// Lookup bindings for selected column
	public bool SelectedColumnHasLookup
	{
		get => SelectedColumn?.Filter?.Lookup != null;
		set
		{
			if (SelectedColumn?.Filter == null) return;

			// lookup je “rozšíření filtru” -> smí existovat jen když je filtr enabled
			if (!SelectedColumn.Filterable)
			{
				SelectedColumn.Filter.Lookup = null;
				OnPropertyChanged();
				OnPropertyChanged(nameof(SelectedLookupCommandText));
				OnPropertyChanged(nameof(SelectedLookupKeyColumn));
				OnPropertyChanged(nameof(SelectedLookupTextColumn));
				return;
			}

			if (value)
			{
				SelectedColumn.Filter.Lookup ??= new LookupConfigUi
				{
					Mode = LookupMode.Sql,
					Sql = new SqlLookupUi()
				};
			}
			else
			{
				SelectedColumn.Filter.Lookup = null;
			}

			OnPropertyChanged();
			OnPropertyChanged(nameof(SelectedLookupCommandText));
			OnPropertyChanged(nameof(SelectedLookupKeyColumn));
			OnPropertyChanged(nameof(SelectedLookupTextColumn));
		}
	}

	public string SelectedLookupCommandText
	{
		get => SelectedColumn?.Filter?.Lookup?.Sql?.CommandText ?? "";
		set
		{
			if (SelectedColumn?.Filter == null) return;
			if (!SelectedColumn.Filterable) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigUi { Mode = LookupMode.Sql, Sql = new SqlLookupUi() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupUi();
			SelectedColumn.Filter.Lookup.Sql.CommandText = value;
			OnPropertyChanged();
		}
	}
	public string SelectedLookupKeyColumn
	{
		get => SelectedColumn?.Filter?.Lookup?.Sql?.KeyColumn ?? "Id";
		set
		{
			if (SelectedColumn?.Filter == null) return;
			if (!SelectedColumn.Filterable) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigUi { Mode = LookupMode.Sql, Sql = new SqlLookupUi() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupUi();
			SelectedColumn.Filter.Lookup.Sql.KeyColumn = value;
			OnPropertyChanged();
		}
	}
	public string SelectedLookupTextColumn
	{
		get => SelectedColumn?.Filter?.Lookup?.Sql?.TextColumn ?? "Name";
		set
		{
			if (SelectedColumn?.Filter == null) return;
			if (!SelectedColumn.Filterable) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigUi { Mode = LookupMode.Sql, Sql = new SqlLookupUi() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupUi();
			SelectedColumn.Filter.Lookup.Sql.TextColumn = value;
			OnPropertyChanged();
		}
	}

	public RelayCommand OpenFolderCommand { get; }
	public RelayCommand NewReportCommand { get; }
	public RelayCommand SaveGenerateCommand { get; }
	public RelayCommand ApplyToDbCommand { get; }
	public RelayCommand ImportColumnsCommand { get; }
	public RelayCommand AddColumnCommand { get; }
	public RelayCommand RemoveSelectedColumnCommand { get; }
	public RelayCommand AddCultureCommand { get; }
	public RelayCommand RemoveCultureCommand { get; }
    public RelayCommand RegenerateTextsCommand { get; }
    public RelayCommand AddPresetCommand { get; }
	public RelayCommand RemovePresetCommand { get; }

	public MainViewModel()
	{
		OpenFolderCommand = new RelayCommand(OpenFolder);
		NewReportCommand = new RelayCommand(NewReport);
		SaveGenerateCommand = new RelayCommand(SaveGenerate);
		ApplyToDbCommand = new RelayCommand(ApplyToDb);
		ImportColumnsCommand = new RelayCommand(ImportColumnsFromDb);
		AddColumnCommand = new RelayCommand(AddColumn);
		RemoveSelectedColumnCommand = new RelayCommand(RemoveSelectedColumn);
		AddCultureCommand = new RelayCommand(AddCulture);
		RemoveCultureCommand = new RelayCommand(RemoveCulture);
        RegenerateTextsCommand = new RelayCommand(RegenerateTexts);
        AddPresetCommand = new RelayCommand(AddPreset);
		RemovePresetCommand = new RelayCommand(RemovePreset);
		ColumnEditor.ColumnTypeValues = ColumnTypeValues;

		var defaultReports = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
		if (Directory.Exists(defaultReports))
			LoadFolder(defaultReports);
	}

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
            
			CultureKeys.Clear();
			foreach (var c in Current.Definition.Texts.Keys.OrderBy(x => x))
				CultureKeys.Add(c);

			if (SelectedCultureKey == null)
				SelectedCultureKey = CultureKeys.FirstOrDefault() ?? Current.Definition.DefaultCulture;

			SelectedPreset = Current.SystemPresets.FirstOrDefault();
			// pass UI model into preset editor
			PresetEditor.Load((ReportDefinitionUi)Current.Definition, SelectedPreset);
			// map selected column to UI model
			SelectedColumn = Current.Definition.Columns.FirstOrDefault() is var firstCol && firstCol != null ? (ReportColumnUi)firstCol : null;

			GeneratedSql = ReportSqlGenerator.GenerateSql(Current);
			StatusText = $"Loaded: {Path.GetFileName(path)}";
			OnPropertyChanged(nameof(SelectedCultureTitle));
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
			ReportName = "New report",
			ViewSchema = "dbo",
			ViewName = "v_YourView",
			Version = 1,
			Definition = new ReportDefinitionUi 
			{ 
				Version = 1, 
				DefaultCulture = Constants.DefaultLanguage, 
				Columns = [],
				DefaultSort = [],
				Texts = []
			},
			SystemPresets = []
		};

		Current.Definition.Texts[Constants.DefaultLanguage] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["report.title"] = Current.ReportName
        };
		CultureKeys.Clear();
		CultureKeys.Add(Constants.DefaultLanguage);
		SelectedCultureKey = Constants.DefaultLanguage;
		CultureEntries.Clear();

		GeneratedSql = ReportSqlGenerator.GenerateSql(Current);
		StatusText = "New report created (not saved yet).";
	}

	private void SaveGenerate()
	{
		try
		{
			if (Current == null) return;

			CommitCultureEntries();

			// Commit preset editor UI into typed content
			if (SelectedPreset != null)
				SelectedPreset.Content = PresetEditor.BuildContent();

			foreach (var p in Current.SystemPresets)
			{
				if (string.IsNullOrWhiteSpace(p.PresetKey))
					throw new InvalidOperationException("PresetKey cannot be empty.");
				p.PresetId = GuidUtil.FromPresetKey(p.PresetKey);
			}

			ValidateLookupSqls();

			GeneratedSql = ReportSqlGenerator.GenerateSql(Current);

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

			CommitCultureEntries();
			if (SelectedPreset != null)
				SelectedPreset.Content = PresetEditor.BuildContent();
			ValidateLookupSqls();
			GeneratedSql = ReportSqlGenerator.GenerateSql(Current);

			StatusText = "Applying to DB...";
			await SqlBatchExecutor.ExecuteScriptAsync(ApplyConnectionString, GeneratedSql);
			StatusText = "Apply complete.";
		}
		catch (Exception ex)
		{
			StatusText = "Apply error: " + ex.Message;
		}
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

			Current.Definition.Columns.Clear();
			foreach (var col in cols)
			{
				var type = DbIntrospector.MapSqlType(col.SqlType);
				var textKey = KnownTextKeys.GetColumnHeaderKey(col.Name);
				Current.Definition.Columns.Add(new ReportColumnUi
				{
					Key = col.Name,
					Type = type,
				});

				EnsureCulture(Current.Definition.DefaultCulture);
				var dict = Current.Definition.Texts[Current.Definition.DefaultCulture];
				if (!dict.ContainsKey(textKey))
					dict[textKey] = Humanize(col.Name);
			}

			LoadCultureEntries();
			SelectedColumn = Current.Definition.Columns.FirstOrDefault();
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

		Current.Definition.Columns.Add(new ReportColumnUi
		{
			Key = "new_column",
			Type = ReportColumnType.String,
		});
		StatusText = "Column added.";
	}

	private void RemoveSelectedColumn()
	{
		if (Current?.Definition == null) return;
		if (SelectedColumn == null) return;
		// remove underlying json column by key
		var toRemove = Current.Definition.Columns.FirstOrDefault(c => string.Equals(c.Key, SelectedColumn.Key, StringComparison.OrdinalIgnoreCase));
		if (toRemove != null) Current.Definition.Columns.Remove(toRemove);
		SelectedColumn = Current.Definition.Columns.FirstOrDefault();
		StatusText = "Column removed.";
	}

	private void AddCulture()
	{
		var key = Microsoft.VisualBasic.Interaction.InputBox("Culture key (e.g. cs, en, pl):", "Add culture", "en").Trim();
		if (key.Length == 0) return;
		EnsureCulture(key);
		if (!CultureKeys.Contains(key)) CultureKeys.Add(key);
		SelectedCultureKey = key;
		StatusText = "Culture added.";
	}

	private void RemoveCulture()
	{
		if (Current?.Definition == null) return;
		if (SelectedCultureKey == null) return;
		if (SelectedCultureKey.Equals(Current.Definition.DefaultCulture, StringComparison.OrdinalIgnoreCase))
		{
			StatusText = "Can't remove DefaultCulture.";
			return;
		}

		Current.Definition.Texts.Remove(SelectedCultureKey);
		CultureKeys.Remove(SelectedCultureKey);
		SelectedCultureKey = CultureKeys.FirstOrDefault();
		StatusText = "Culture removed.";
	}

    private void RegenerateTexts()
    {
        // Ensure that all columns have text entries in all cultures
        if (Current?.Definition == null) return;
		
		var expectedTextKeys = new Dictionary<string, string>()
		{
			{ KnownTextKeys.ReportTitle, Current.ReportName }
		};

		foreach (var col in Current.Definition.Columns)
        {
			expectedTextKeys[KnownTextKeys.GetColumnHeaderKey(col.Key)] = Humanize(col.Key);
        }

        EnsureCulture(Current.Definition.DefaultCulture ?? Constants.DefaultLanguage);

        // For each culture, ensure all expected text keys exist and remove any unknown keys
        foreach (var culture in Current.Definition.Texts.Keys)
        {
            // Remove unknown keys
            var cultureTexts = Current.Definition.Texts[culture];
			foreach (var textKey in cultureTexts.Keys.ToList())
			{
				if (!expectedTextKeys.ContainsKey(textKey))
				{
					cultureTexts.Remove(textKey);
                }
			}

			// Add missing keys
			foreach (var kv in expectedTextKeys)
			{
				if (!cultureTexts.ContainsKey(kv.Key))
				{
					cultureTexts[kv.Key] = kv.Value;
				}
			}
        }

        LoadCultureEntries();
        StatusText = "Regenerated missing text entries for columns.";
    }

    private void EnsureCulture(string key)
	{
		if (Current?.Definition == null) return;
		if (!Current.Definition.Texts.ContainsKey(key))
			Current.Definition.Texts[key] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	}

	private void LoadCultureEntries()
	{
		if (Current?.Definition == null) return;
		CultureEntries.Clear();
		if (SelectedCultureKey == null) return;
		EnsureCulture(SelectedCultureKey);

		foreach (var kv in Current.Definition.Texts[SelectedCultureKey].OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
			CultureEntries.Add(new KvRowVm { Key = kv.Key, Value = kv.Value });

		OnPropertyChanged(nameof(SelectedCultureTitle));
	}

	private void CommitCultureEntries()
	{
		if (Current?.Definition == null) return;
		if (SelectedCultureKey == null) return;
		EnsureCulture(SelectedCultureKey);

		var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var row in CultureEntries)
		{
			var k = (row.Key ?? string.Empty).Trim();
			if (k.Length == 0) continue;
			dict[k] = row.Value ?? string.Empty;
		}
		Current.Definition.Texts[SelectedCultureKey] = dict;
	}

	private void AddPreset()
	{
		if (Current == null) return;
		var key = $"{Current.ReportKey}_{Guid.NewGuid():N}";
		var p = new SystemPresetUi
		{
			PresetKey = key,
			Name = "New preset",
			IsDefault = Current.SystemPresets.Count == 0,
			PresetId = GuidUtil.FromPresetKey(key),
			Content = new PresetContentUi()
		};
		Current.SystemPresets.Add(p);
		SelectedPreset = p;
		StatusText = "Preset added.";
	}

	private void RemovePreset()
	{
		if (Current == null) return;
		if (SelectedPreset == null) return;
		Current.SystemPresets.Remove(SelectedPreset);
		SelectedPreset = Current.SystemPresets.FirstOrDefault();
		// reload preset editor with UI model
		PresetEditor.Load(Current.Definition, SelectedPreset);
		StatusText = "Preset removed.";
	}

	private void ValidateLookupSqls()
	{
		if (Current?.Definition == null) return;

		var errors = new List<string>();
		foreach (var column in Current.Definition.Columns)
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
}
