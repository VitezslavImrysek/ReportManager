using ReportAdmin.Core.Models;
using ReportAdmin.Core.Utils;
using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Text;

namespace ReportAdmin.Core.Sql;

public static class ReportSqlGenerator
{
	public static string GenerateSql(ReportSqlDocumentUi doc)
	{
		foreach (var p in doc.SystemPresets)
			p.PresetId = GuidUtil.FromPresetKey(p.PresetKey);

		var presets = doc.SystemPresets
			.OrderBy(p => p.PresetKey, StringComparer.OrdinalIgnoreCase)
			.ToList();

		// enforce single default
		var defaultPreset = presets.FirstOrDefault(p => p.IsDefault) ?? presets.FirstOrDefault();
		foreach (var p in presets)
			p.IsDefault = defaultPreset != null && p.PresetKey.Equals(defaultPreset.PresetKey, StringComparison.OrdinalIgnoreCase);

		var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
		var sb = new StringBuilder();

		sb.AppendLine($"/* REPORT: {doc.ReportKey} */");
		sb.AppendLine($"/* GENERATED: {now} */");
		sb.AppendLine("/* DO NOT EDIT BY HAND */");
		sb.AppendLine();
		sb.AppendLine("BEGIN TRY");
		sb.AppendLine("BEGIN TRAN;");
		sb.AppendLine();
		sb.AppendLine($"DECLARE @ReportKey nvarchar(100) = N'{Esc(doc.ReportKey)}';");
		sb.AppendLine($"DECLARE @ReportName nvarchar(200) = N'{Esc(doc.ReportName)}';");
		sb.AppendLine($"DECLARE @ViewSchema nvarchar(128) = N'{Esc(doc.ViewSchema)}';");
		sb.AppendLine($"DECLARE @ViewName   nvarchar(128) = N'{Esc(doc.ViewName)}';");
		sb.AppendLine($"DECLARE @Version int = {doc.Version};");
		sb.AppendLine();
		sb.AppendLine("/* === ReportDefinitionJson BEGIN === */");
		var defJson = JsonUtil.Serialize((ReportDefinitionJson)doc.Definition);
		sb.AppendLine($"DECLARE @DefinitionJson nvarchar(max) = N'{Esc(defJson)}';");
		sb.AppendLine("/* === ReportDefinitionJson END === */");
		sb.AppendLine();
		sb.AppendLine("-- Upsert ReportDefinition");
		sb.AppendLine(@"MERGE dbo.ReportDefinition AS t
USING (SELECT @ReportKey AS [Key]) AS s
ON t.[Key] = s.[Key]
WHEN MATCHED THEN
  UPDATE SET
	t.[Name] = @ReportName,
	t.ViewSchema = @ViewSchema,
	t.ViewName = @ViewName,
	t.DefinitionJson = @DefinitionJson,
	t.Version = @Version,
	t.IsActive = 1,
	t.UpdatedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
  INSERT ([Key],[Name],ViewSchema,ViewName,DefinitionJson,Version,IsActive)
  VALUES (@ReportKey,@ReportName,@ViewSchema,@ViewName,@DefinitionJson,@Version,1);");
		sb.AppendLine();
		sb.AppendLine("-- System presets (OwnerUserId IS NULL)");
		sb.AppendLine("/* === SystemPresets BEGIN === */");
		sb.AppendLine();

		int i = 1;
		foreach (var p in presets)
		{
			var idx = i.ToString();
			sb.AppendLine($"-- preset: {p.PresetKey}");
			sb.AppendLine($"DECLARE @PresetKey_{idx} nvarchar(100) = N'{Esc(p.PresetKey)}';");
			sb.AppendLine($"DECLARE @PresetName_{idx} nvarchar(200) = N'{Esc(p.Name)}';");
			sb.AppendLine($"DECLARE @PresetId_{idx} uniqueidentifier = '{p.PresetId}';");
			sb.AppendLine($"DECLARE @IsDefault_{idx} bit = {(p.IsDefault ? 1 : 0)};");
			sb.AppendLine();
			var pJson = JsonUtil.Serialize((PresetContentJson)p.Content);
			sb.AppendLine($"DECLARE @PresetJson_{idx} nvarchar(max) = N'{Esc(pJson)}';");
			sb.AppendLine();
			sb.AppendLine(@"MERGE dbo.ReportViewPreset AS pv
USING (SELECT @PresetId_" + idx + @" AS PresetId) AS s
ON pv.PresetId = s.PresetId
WHEN MATCHED THEN
  UPDATE SET
	pv.ReportKey = @ReportKey,
	pv.[Name] = @PresetName_" + idx + @",
	pv.OwnerUserId = NULL,
	pv.PresetJson = @PresetJson_" + idx + @",
	pv.IsDefault = @IsDefault_" + idx + @",
	pv.UpdatedUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN
  INSERT (PresetId, ReportKey, [Name], OwnerUserId, PresetJson, IsDefault, CreatedUtc, UpdatedUtc)
  VALUES (@PresetId_" + idx + @", @ReportKey, @PresetName_" + idx + @", NULL, @PresetJson_" + idx + @", @IsDefault_" + idx + @", SYSUTCDATETIME(), SYSUTCDATETIME());");
			sb.AppendLine();
			i++;
		}

		if (presets.Count > 0)
		{
			sb.AppendLine("-- enforce single default (system)");
			sb.AppendLine("UPDATE dbo.ReportViewPreset");
			sb.AppendLine($"SET IsDefault = CASE WHEN PresetId = '{defaultPreset!.PresetId}' THEN 1 ELSE 0 END");
			sb.AppendLine("WHERE ReportKey = @ReportKey AND OwnerUserId IS NULL;");
			sb.AppendLine();
		}

		sb.AppendLine("/* === SystemPresets END === */");
		sb.AppendLine();
		sb.AppendLine("COMMIT;");
		sb.AppendLine("END TRY");
		sb.AppendLine("BEGIN CATCH");
		sb.AppendLine("  IF @@TRANCOUNT > 0 ROLLBACK;");
		sb.AppendLine("  THROW;");
		sb.AppendLine("END CATCH");
		sb.AppendLine("GO");
		return sb.ToString();
	}

	private static string Esc(string s) => (s ?? string.Empty).Replace("'", "''");
}
