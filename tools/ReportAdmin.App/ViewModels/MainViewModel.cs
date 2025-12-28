using Microsoft.Win32;
using ReportAdmin.App.Dialogs;
using ReportAdmin.Core.Db;
using ReportAdmin.Core.Models;
using ReportAdmin.Core.Sql;
using ReportAdmin.Core.Utils;
using ReportManager.ApiContracts;
using ReportManager.ApiContracts.Dto;
using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace ReportAdmin.App.ViewModels;

public sealed class MainViewModel : NotificationObject
{
	public PresetEditorViewModel PresetEditor { get; } = new();

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

	private ReportSqlDocument? _current;
	public ReportSqlDocument? Current
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

	public ReportColumnJson? SelectedColumn
	{
		get;
		set
		{
			if (SetValue(ref field, value))
			{
				OnPropertyChanged(nameof(SelectedColumnHasLookup));
				OnPropertyChanged(nameof(SelectedLookupCommandText));
				OnPropertyChanged(nameof(SelectedLookupKeyColumn));
				OnPropertyChanged(nameof(SelectedLookupTextColumn));
			}
		}
	}

	public SystemPreset? SelectedPreset
	{
		get;
		set
		{
			if (SetValue(ref field, value))
				RefreshPresetJson();
		}
	}

	public string SelectedPresetJson
	{
		get;
		set
		{
			if (SetValue(ref field, value))
			{
				if (SelectedPreset != null)
				{
					try
					{
						var m = JsonUtil.Deserialize<PresetContentJson>(value);
						if (m != null) SelectedPreset.Content = m;
					}
					catch { }
				}
			}
		}
	} = string.Empty;

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
			if (!SelectedColumn.Filter.Enabled)
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
				SelectedColumn.Filter.Lookup ??= new LookupConfigJson
				{
					Mode = LookupMode.Sql,
					Sql = new SqlLookupJson()
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
			if (!SelectedColumn.Filter.Enabled) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigJson { Mode = LookupMode.Sql, Sql = new SqlLookupJson() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupJson();
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
			if (!SelectedColumn.Filter.Enabled) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigJson { Mode = LookupMode.Sql, Sql = new SqlLookupJson() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupJson();
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
			if (!SelectedColumn.Filter.Enabled) return;

			SelectedColumn.Filter.Lookup ??= new LookupConfigJson { Mode = LookupMode.Sql, Sql = new SqlLookupJson() };
			SelectedColumn.Filter.Lookup.Sql ??= new SqlLookupJson();
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
		AddPresetCommand = new RelayCommand(AddPreset);
		RemovePresetCommand = new RelayCommand(RemovePreset);

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
			string folderPath = System.IO.Path.GetDirectoryName(dialog.FileName);
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
			Current.Definition ??= new ReportDefinitionJson();

			CultureKeys.Clear();
			foreach (var c in Current.Definition.Texts.Keys.OrderBy(x => x))
				CultureKeys.Add(c);

			if (SelectedCultureKey == null)
				SelectedCultureKey = CultureKeys.FirstOrDefault() ?? Current.Definition.DefaultCulture;

			SelectedPreset = Current.SystemPresets.FirstOrDefault();
			PresetEditor.Load(Current.Definition, SelectedPreset);
			SelectedColumn = Current.Definition.Columns.FirstOrDefault();

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
		Current = new ReportSqlDocument
		{
			ReportKey = "NewReport",
			ReportName = "New report",
			ViewSchema = "dbo",
			ViewName = "v_YourView",
			Version = 1,
			Definition = new ReportDefinitionJson { Version = 1, DefaultCulture = Constants.DefaultLanguage, TextKey = "report.title" },
			SystemPresets = new List<SystemPreset>()
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
			foreach (var c in cols)
			{
				var type = DbIntrospector.MapSqlType(c.SqlType);
				var textKey = $"col.{ToPascal(c.Name)}";
				Current.Definition.Columns.Add(new ReportColumnJson
				{
					Key = c.Name,
					TextKey = textKey,
					Type = type,
					Hidden = false,
					AlwaysSelect = false,
					Filter = new FilterConfigJson { Enabled = true },
					Sort = new SortConfigJson { Enabled = true }
				});

				EnsureCulture(Current.Definition.DefaultCulture);
				var dict = Current.Definition.Texts[Current.Definition.DefaultCulture];
				if (!dict.ContainsKey(textKey))
					dict[textKey] = Humanize(c.Name);
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

		Current.Definition.Columns.Add(new ReportColumnJson
		{
			Key = "new_column",
			TextKey = "col.NewColumn",
			Type = ReportColumnType.String,
			Filter = new FilterConfigJson { Enabled = true },
			Sort = new SortConfigJson { Enabled = true }
		});
		StatusText = "Column added.";
	}

	private void RemoveSelectedColumn()
	{
		if (Current?.Definition == null) return;
		if (SelectedColumn == null) return;
		Current.Definition.Columns.Remove(SelectedColumn);
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
			var k = (row.Key ?? "").Trim();
			if (k.Length == 0) continue;
			dict[k] = row.Value ?? "";
		}
		Current.Definition.Texts[SelectedCultureKey] = dict;
	}

	private void AddPreset()
	{
		if (Current == null) return;
		var key = $"{Current.ReportKey}_{Guid.NewGuid():N}";
		var p = new SystemPreset
		{
			PresetKey = key,
			Name = "New preset",
			IsDefault = Current.SystemPresets.Count == 0,
			PresetId = GuidUtil.FromPresetKey(key),
			Content = new PresetContentJson()
		};
		Current.SystemPresets.Add(p);
		SelectedPreset = p;
		RefreshPresetJson();
		StatusText = "Preset added.";
	}

	private void RemovePreset()
	{
		if (Current == null) return;
		if (SelectedPreset == null) return;
		Current.SystemPresets.Remove(SelectedPreset);
		SelectedPreset = Current.SystemPresets.FirstOrDefault();
		PresetEditor.Load(Current.Definition, SelectedPreset);
		RefreshPresetJson();
		StatusText = "Preset removed.";
	}

	private void RefreshPresetJson()
	{
		SelectedPresetJson = SelectedPreset == null ? "" : JsonUtil.Serialize(SelectedPreset.Content);
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
	private static string ToPascal(string s)
	{
		if (string.IsNullOrWhiteSpace(s)) return s;
		if (s.Contains('_'))
		{
			var parts = s.Split(["_"], StringSplitOptions.RemoveEmptyEntries);
			return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
		}
		return char.ToUpperInvariant(s[0]) + s[1..];
	}
}
