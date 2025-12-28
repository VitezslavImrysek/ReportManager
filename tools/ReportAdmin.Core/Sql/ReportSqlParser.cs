using Microsoft.SqlServer.TransactSql.ScriptDom;
using ReportAdmin.Core.Models;
using ReportManager.DefinitionModel.Json;
using ReportManager.DefinitionModel.Models.ReportDefinition;
using ReportManager.DefinitionModel.Models.ReportPreset;
using System.Globalization;
using System.Text;

namespace ReportAdmin.Core.Sql;

public static class ReportSqlParser
{
	public static ReportSqlDocument LoadFromFile(string path)
	{
		var sql = File.ReadAllText(path, Encoding.UTF8);
		var doc = Parse(sql);
		doc.FilePath = path;
		return doc;
	}

	public static ReportSqlDocument Parse(string sql)
	{
		var parser = new TSql160Parser(initialQuotedIdentifiers: true);
		IList<ParseError> errors;
		var fragment = parser.Parse(new StringReader(sql), out errors);

		if (errors is { Count: > 0 })
		{
			var msg = string.Join(Environment.NewLine, errors.Select(e => $"{e.Line},{e.Column}: {e.Message}"));
			throw new InvalidOperationException("SQL parse error(s):\n" + msg);
		}

		var declares = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
		fragment.Accept(new DeclareCollector(declares));

		string GetStr(string name, string fallback = "") =>
			declares.TryGetValue(name, out var v) ? Convert.ToString(v, CultureInfo.InvariantCulture) ?? fallback : fallback;

		int GetInt(string name, int fallback = 1) =>
			declares.TryGetValue(name, out var v) && int.TryParse(Convert.ToString(v, CultureInfo.InvariantCulture), out var i) ? i : fallback;

		var model = new ReportSqlDocument
		{
			ReportKey = GetStr("@ReportKey"),
			ReportName = GetStr("@ReportName"),
			ViewSchema = GetStr("@ViewSchema", "dbo"),
			ViewName = GetStr("@ViewName"),
			Version = GetInt("@Version", 1),
		};

		var defJson = GetStr("@DefinitionJson");
		model.Definition = JsonUtil.Deserialize<ReportDefinitionJson>(defJson) ?? new ReportDefinitionJson();

		// Presets: indexed variables
		var idxs = declares.Keys
			.Where(k => k.StartsWith("@PresetKey_", StringComparison.OrdinalIgnoreCase))
			.Select(k => k.Split('_').Last())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();

		foreach (var idx in idxs)
		{
			var key = GetStr("@PresetKey_" + idx);
			if (string.IsNullOrWhiteSpace(key)) continue;

			var name = GetStr("@PresetName_" + idx, key);
			var idStr = GetStr("@PresetId_" + idx, "");
			var isDefStr = GetStr("@IsDefault_" + idx, "0");
			var json = GetStr("@PresetJson_" + idx, "{}");

			var content = JsonUtil.Deserialize<PresetContentJson>(json) ?? new PresetContentJson();

			model.SystemPresets.Add(new SystemPreset
			{
				PresetKey = key,
				Name = name,
				PresetId = Guid.TryParse(idStr, out var g) ? g : Guid.Empty,
				IsDefault = isDefStr.Equals("1") || isDefStr.Equals("true", StringComparison.OrdinalIgnoreCase),
				Content = content
			});
		}

		return model;
	}

	private sealed class DeclareCollector : TSqlFragmentVisitor
	{
		private readonly Dictionary<string, object?> _declares;
		public DeclareCollector(Dictionary<string, object?> declares) => _declares = declares;

		public override void Visit(DeclareVariableStatement node)
		{
			foreach (var decl in node.Declarations)
			{
				var name = decl.VariableName.Value;
				_declares[name] = decl.Value == null ? null : EvalScalar(decl.Value);
			}
		}

		private static object? EvalScalar(ScalarExpression expr) => expr switch
		{
			StringLiteral s => s.Value,
			//UnicodeStringLiteral u => u.Value,
			IntegerLiteral i => i.Value,
			NumericLiteral n => n.Value,
			MoneyLiteral m => m.Value,
			BinaryLiteral b => b.Value,
			NullLiteral => null,
			CastCall c => c.Parameter != null ? EvalScalar(c.Parameter) : null,
			_ => expr.ToString()
		};
	}
}
