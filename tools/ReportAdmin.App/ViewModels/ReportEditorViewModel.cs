using ReportAdmin.App.Dialogs;
using ReportAdmin.App.Messages;
using ReportAdmin.Core.Db;
using ReportAdmin.Core.Models;
using ReportAdmin.Core.Models.Definition;
using ReportAdmin.Core.Sql;
using ReportManager.DefinitionModel.Utils;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using System.Windows;

namespace ReportAdmin.App.ViewModels;

public class ReportEditorContext
{
    public required string ReportFolder { get; init; }
}

public sealed class ReportEditorViewModel : DataEditorVM<ReportFileItem, ReportEditorContext>
{
    private string? _filePath;

    #region Ctor

    public ReportEditorViewModel()
    {
        SaveGenerateCommand = new RelayCommand(SaveGenerate);
        ApplyToDbCommand = new RelayCommand(ApplyToDb);

		ReportColumnsEditorVM = new ReportColumnsEditorViewModel();
        DefaultSortVM = new SortViewModel();
        SystemPresetsEditorVM = new SystemPresetsEditorViewModel();
        ReportHeaderVM = new ReportHeaderViewModel() { ImportColumnsCommand = new RelayCommand(ImportColumnsFromDb) };
        ReportTextsEditorVM = new TextsEditorViewModel() { Mode = TextsEditorMode.Report };
    }

    #endregion

    #region Properties

    public string? RepoPath { get; set => SetValue(ref field, value); }
    public string GeneratedSql { get; set => SetValue(ref field, value); } = string.Empty;
	public string ApplyConnectionString { get; set => SetValue(ref field, value); } = string.Empty;

    #endregion

    #region Commands

	public RelayCommand SaveGenerateCommand { get; }
	public RelayCommand ApplyToDbCommand { get; }

    #endregion

    #region ViewModels

	public ReportColumnsEditorViewModel ReportColumnsEditorVM { get; set => SetValue(ref field, value); }
    public SortViewModel DefaultSortVM { get; set => SetValue(ref field, value); }
    public SystemPresetsEditorViewModel SystemPresetsEditorVM { get; set => SetValue(ref field, value); }
    public TextsEditorViewModel ReportTextsEditorVM { get; set => SetValue(ref field, value); }
    public ReportHeaderViewModel ReportHeaderVM { get; set => SetValue(ref field, value); }

    #endregion

    #region Protected Override Methods

    protected override void OnSetData(ReportFileItem data)
    {
        _filePath = data.FilePath;
        RepoPath = Path.GetDirectoryName(data.FilePath);

        try
        {
            var report = ReportSqlParser.LoadFromFile(data.FilePath);
            if (report.Definition == null)
            {
                // Contains no or invalid definition.
                MessageBox.Show("The report SQL file does not contain a valid report definition.");
                return;
            }

            SetData(report);
            NotifyStatus($"Loaded: {Path.GetFileName(data.FilePath)}");
        }
        catch (Exception ex)
        {
            NotifyStatus("Load error: " + ex.Message);
        }
    }

    protected override void OnGetData(ReportFileItem data)
    {
		data.FilePath = _filePath;
        SaveGenerate();
    }

    protected override void OnNew(ReportEditorContext context)
    {
        RepoPath = context.ReportFolder;

        var report = new ReportSqlDocumentUi
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

        report.Definition.Texts[Constants.DefaultLanguage] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["report.title"] = "New report"
        };

        SetData(report);
        NotifyStatus("New report created (not saved yet).");
    }

    #endregion

    #region Private Methods

    private void SetData(ReportSqlDocumentUi report)
    {
        ReportHeaderVM.SetData(report);

        ReportTextsEditorVM.DefaultCulture = report.Definition.DefaultCulture;
        ReportTextsEditorVM.SetData(report.Definition.Texts);

        ReportColumnsEditorVM.SetData(report.Definition.Columns);
        DefaultSortVM.SetData(report.Definition.DefaultSort);
        SystemPresetsEditorVM.SetData(report.SystemPresets);

        GeneratedSql = ReportSqlGenerator.GenerateSql(report);
    }

    private ReportSqlDocumentUi? GetData()
    {
        var isOK = Validate();
        if (!isOK)
        {
            return null;
        }

        var report = new ReportSqlDocumentUi()
        {
            Definition = new ReportDefinitionUi()
        };

        ReportHeaderVM.GetData(report);

        report.Definition.Columns = [];
        ReportColumnsEditorVM.GetData(report.Definition.Columns);

        report.Definition.DefaultSort = [];
        DefaultSortVM.GetData(report.Definition.DefaultSort);

        report.Definition.Texts = [];
        ReportTextsEditorVM.GetData(report.Definition.Texts);

        report.SystemPresets = [];
        SystemPresetsEditorVM.GetData(report.SystemPresets);

        return report;
    }

    private void SaveGenerate()
	{
		try
		{
			var report = GetData(); 
            if (report == null)
            {
                return;
            }

            ValidateReportDefinition(report);
            ValidateLookupSqls(report);

			GeneratedSql = ReportSqlGenerator.GenerateSql(report);

			if (string.IsNullOrWhiteSpace(RepoPath))
				throw new InvalidOperationException("Open folder first.");

			var file = Path.Combine(RepoPath, report.ReportKey + ".sql");
			File.WriteAllText(file, GeneratedSql);

            SendMessage<RefreshReportsMessage>();
            NotifyStatus($"Saved: {file}");

		}
		catch (Exception ex)
		{
            NotifyStatus("Save error: " + ex.Message);
		}
	}

    private async void ApplyToDb()
	{
		try
		{
			if (string.IsNullOrWhiteSpace(ApplyConnectionString))
				throw new InvalidOperationException("Connection string is empty.");

            var report = GetData();
            if (report == null)
            {
                return;
            }

            ValidateReportDefinition(report);
			ValidateLookupSqls(report);

            GeneratedSql = ReportSqlGenerator.GenerateSql(report);

            NotifyStatus("Applying to DB...");
			await SqlBatchExecutor.ExecuteScriptAsync(ApplyConnectionString, GeneratedSql);
            NotifyStatus("Apply complete.");
		}
		catch (Exception ex)
		{
            NotifyStatus("Apply error: " + ex.Message);
		}
	}

    private async void ImportColumnsFromDb()
	{
		try
		{
			var dlgVM = new ImportDialogViewModel
			{
				ConnStringText = ApplyConnectionString,
				SchemaText = ReportHeaderVM.ViewSchema ?? string.Empty,
				ViewText = ReportHeaderVM.ViewName ?? string.Empty
			};

			var dlg = new ImportDialog { DataContext = dlgVM, Owner = Application.Current.MainWindow };
			if (dlg.ShowDialog() != true) return;

			ApplyConnectionString = dlgVM.ConnStringText;
			
            NotifyStatus("Reading view metadata...");
			var cols = await DbIntrospector.GetViewColumnsAsync(dlgVM.ConnStringText, dlgVM.SchemaText, dlgVM.ViewText);

			ReportColumnsEditorVM.UpdateColumns(cols);

            var report = GetData();
            if (report == null)
            {
                return;
            }

            GeneratedSql = ReportSqlGenerator.GenerateSql(report);
            NotifyStatus($"Imported {cols.Count} columns from {dlgVM.SchemaText}.{dlgVM.ViewText}.");
		}
		catch (Exception ex)
		{
            NotifyStatus("Import error: " + ex.Message);
		}
	}

	private static void ValidateLookupSqls(ReportSqlDocumentUi document)
	{
		if (document?.Definition == null) return;

		var errors = new List<string>();
		foreach (var column in document.Definition.Columns)
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

    private static void ValidateReportDefinition(ReportSqlDocumentUi document)
    {
        var errors = new List<string>();
        var definition = document.Definition;
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

        foreach (var col in definition.Columns)
        {
            expectedTextKeys[KnownTextKeys.GetColumnHeaderKey(col.Key)] = Humanize(col.Key);
        }

        // For each culture, ensure all expected text keys exist and remove any unknown keys
        foreach (var culture in definition.Texts.Keys)
        {
            // Remove unknown keys
            var cultureTexts = definition.Texts[culture];
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
}
