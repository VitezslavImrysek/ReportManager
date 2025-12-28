using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace ReportAdmin.Core.Sql;

public static class SqlBatchExecutor
{
	public static List<string> SplitBatches(string sql)
	{
		var lines = sql.Replace("\r\n", "\n").Split('\n');
		var batches = new List<string>();
		var current = new List<string>();

		foreach (var line in lines)
		{
			if (Regex.IsMatch(line, @"^\s*GO\s*$", RegexOptions.IgnoreCase))
			{
				batches.Add(string.Join("\n", current));
				current.Clear();
			}
			else current.Add(line);
		}

		if (current.Count > 0)
			batches.Add(string.Join("\n", current));

		return batches;
	}

	public static async Task ExecuteScriptAsync(string connectionString, string sql, CancellationToken ct = default)
	{
		var batches = SplitBatches(sql);

		using var con = new SqlConnection(connectionString);
		await con.OpenAsync(ct);

		foreach (var batch in batches)
		{
			if (string.IsNullOrWhiteSpace(batch)) continue;
			using var cmd = con.CreateCommand();
			cmd.CommandTimeout = 120;
			cmd.CommandText = batch;
			await cmd.ExecuteNonQueryAsync(ct);
		}
	}
}
