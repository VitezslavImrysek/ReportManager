using ReportManager.ApiContracts.Dto;
using ReportManager.Server.Utils;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;

namespace ReportManager.Server.Repository
{
	public sealed class SqlQueryBuilder
	{
		public sealed class ColumnInfo
		{
			public required string Key;
			public ReportColumnType Type;
			public bool FilterEnabled;
			public bool SortEnabled;
		}

		public static (string Sql, List<SqlParameter> Parameters) BuildPagedSelect(
			string schema,
			string viewName,
			List<string> selectedColumns,
			List<ColumnInfo> allowedColumns,
			QuerySpecDto query,
			int pageIndex,
			int pageSize)
		{
			var cols = allowedColumns.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);

			// Validate selected columns
			var realCols = (selectedColumns ?? new List<string>()).Where(c => cols.ContainsKey(c)).ToList();
			if (realCols.Count == 0)
				realCols = cols.Keys.ToList(); // fallback

			// SELECT
			var sb = new StringBuilder();
			sb.Append("SELECT ");
			sb.Append(string.Join(", ", realCols.Select(c => SqlUtil.QuoteIdentifier(c))));
			sb.Append(" FROM ").Append(SqlUtil.QuoteIdentifier(schema)).Append(".").Append(SqlUtil.QuoteIdentifier(viewName));

			// WHERE
			var parameters = new List<SqlParameter>();
			var where = new List<string>();
			int p = 0;

			foreach (var f in query.Filters ?? [])
			{
				if (!cols.TryGetValue(f.ColumnKey ?? "", out var col) || !col.FilterEnabled)
					continue;

				// Basic whitelist of operations by type is assumed done earlier; here just implement a subset for demo.
				string colSql = SqlUtil.QuoteIdentifier(col.Key);

				switch (f.Operation)
				{
					case FilterOperation.IsNull:
						where.Add(colSql + " IS NULL");
						break;
					case FilterOperation.NotNull:
						where.Add(colSql + " IS NOT NULL");
						break;
					case FilterOperation.Eq:
					case FilterOperation.Ne:
					case FilterOperation.Gt:
					case FilterOperation.Ge:
					case FilterOperation.Lt:
					case FilterOperation.Le:
						if (f.Values == null || f.Values.Count < 1) break;
						var val = ConvertValue(col.Type, f.Values[0]);
						string op = f.Operation == FilterOperation.Eq ? "=" :
									f.Operation == FilterOperation.Ne ? "<>" :
									f.Operation == FilterOperation.Gt ? ">" :
									f.Operation == FilterOperation.Ge ? ">=" :
									f.Operation == FilterOperation.Lt ? "<" : "<=";
						string pn = "@p" + (p++);
						parameters.Add(new SqlParameter(pn, val ?? DBNull.Value));
						where.Add(colSql + " " + op + " " + pn);
						break;

					case FilterOperation.Between:
						if (f.Values == null || f.Values.Count < 2) break;
						var v1 = ConvertValue(col.Type, f.Values[0]);
						var v2 = ConvertValue(col.Type, f.Values[1]);
						string p1 = "@p" + (p++);
						string p2 = "@p" + (p++);
						parameters.Add(new SqlParameter(p1, v1 ?? DBNull.Value));
						parameters.Add(new SqlParameter(p2, v2 ?? DBNull.Value));
						where.Add(colSql + " BETWEEN " + p1 + " AND " + p2);
						break;

					case FilterOperation.Contains:
					case FilterOperation.NotContains:
					case FilterOperation.StartsWith:
					case FilterOperation.EndsWith:
						if (col.Type != ReportColumnType.String) break;
						if (f.Values == null || f.Values.Count < 1) break;
						string text = f.Values[0] ?? "";
						string like = f.Operation == FilterOperation.Contains ? "%" + text + "%" :
									  f.Operation == FilterOperation.NotContains ? "%" + text + "%" :
									  f.Operation == FilterOperation.StartsWith ? text + "%" :
									  "%" + text;
						string lp = "@p" + (p++);
						parameters.Add(new SqlParameter(lp, like));
						if (f.Operation == FilterOperation.NotContains)
							where.Add(colSql + " NOT LIKE " + lp);
						else
							where.Add(colSql + " LIKE " + lp);
						break;

					case FilterOperation.In:
					case FilterOperation.NotIn:
						if (f.Values == null || f.Values.Count < 1) break;
						// demo: parameters list. For production, switch to TVP for larger lists.
						var vals = f.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
						if (vals.Count == 0) break;
						var inParams = new List<string>();
						foreach (var s in vals)
						{
							string ip = "@p" + (p++);
							parameters.Add(new SqlParameter(ip, ConvertValue(col.Type, s) ?? DBNull.Value));
							inParams.Add(ip);
						}
						string inSql = colSql + (f.Operation == FilterOperation.NotIn ? " NOT IN (" : " IN (") + string.Join(",", inParams) + ")";
						where.Add(inSql);
						break;
				}
			}

			if (where.Count > 0)
				sb.Append(" WHERE ").Append(string.Join(" AND ", where));

			// ORDER BY
			var order = new List<string>();
			foreach (var s in query.Sorting ?? [])
			{
				if (!cols.TryGetValue(s.ColumnKey ?? "", out var col) || !col.SortEnabled)
					continue;
				string dir = s.Direction == SortDirection.Desc ? " DESC" : " ASC";
				order.Add(SqlUtil.QuoteIdentifier(col.Key) + dir);
			}
			if (order.Count == 0)
				order.Add("(SELECT 1)"); // stable order required by OFFSET/FETCH

			sb.Append(" ORDER BY ").Append(string.Join(", ", order));
			sb.Append(" OFFSET ").Append(pageIndex * pageSize).Append(" ROWS FETCH NEXT ").Append(pageSize).Append(" ROWS ONLY;");

			return (sb.ToString(), parameters);
		}

		public static (string Sql, List<SqlParameter> Parameters) BuildCount(
			string schema,
			string viewName,
			List<ColumnInfo> allowedColumns,
			QuerySpecDto query)
		{
			// reuse BuildPagedSelect WHERE logic by calling it with dummy select list and no paging, but simpler here:
			var cols = allowedColumns.ToDictionary(c => c.Key, StringComparer.OrdinalIgnoreCase);
			var sb = new StringBuilder();
			sb.Append("SELECT COUNT(1) FROM ").Append(SqlUtil.QuoteIdentifier(schema)).Append(".").Append(SqlUtil.QuoteIdentifier(viewName));

			var parameters = new List<SqlParameter>();
			var where = new List<string>();
			int p = 0;

			foreach (var f in query.Filters ?? new List<FilterSpecDto>())
			{
				if (!cols.TryGetValue(f.ColumnKey ?? "", out var col) || !col.FilterEnabled)
					continue;

				string colSql = SqlUtil.QuoteIdentifier(col.Key);

				switch (f.Operation)
				{
					case FilterOperation.IsNull:
						where.Add(colSql + " IS NULL");
						break;
					case FilterOperation.NotNull:
						where.Add(colSql + " IS NOT NULL");
						break;
					case FilterOperation.Eq:
					case FilterOperation.Ne:
					case FilterOperation.Gt:
					case FilterOperation.Ge:
					case FilterOperation.Lt:
					case FilterOperation.Le:
						if (f.Values == null || f.Values.Count < 1) break;
						var val = ConvertValue(col.Type, f.Values[0]);
						string op = f.Operation == FilterOperation.Eq ? "=" :
									f.Operation == FilterOperation.Ne ? "<>" :
									f.Operation == FilterOperation.Gt ? ">" :
									f.Operation == FilterOperation.Ge ? ">=" :
									f.Operation == FilterOperation.Lt ? "<" : "<=";
						string pn = "@p" + (p++);
						parameters.Add(new SqlParameter(pn, val ?? DBNull.Value));
						where.Add(colSql + " " + op + " " + pn);
						break;
					case FilterOperation.Between:
						if (f.Values == null || f.Values.Count < 2) break;
						var v1 = ConvertValue(col.Type, f.Values[0]);
						var v2 = ConvertValue(col.Type, f.Values[1]);
						string p1 = "@p" + (p++);
						string p2 = "@p" + (p++);
						parameters.Add(new SqlParameter(p1, v1 ?? DBNull.Value));
						parameters.Add(new SqlParameter(p2, v2 ?? DBNull.Value));
						where.Add(colSql + " BETWEEN " + p1 + " AND " + p2);
						break;
					case FilterOperation.Contains:
					case FilterOperation.NotContains:
					case FilterOperation.StartsWith:
					case FilterOperation.EndsWith:
						if (col.Type != ReportColumnType.String) break;
						if (f.Values == null || f.Values.Count < 1) break;
						string text = f.Values[0] ?? "";
						string like = f.Operation == FilterOperation.Contains ? "%" + text + "%" :
									  f.Operation == FilterOperation.NotContains ? "%" + text + "%" :
									  f.Operation == FilterOperation.StartsWith ? text + "%" :
									  "%" + text;
						string lp = "@p" + (p++);
						parameters.Add(new SqlParameter(lp, like));
						if (f.Operation == FilterOperation.NotContains)
							where.Add(colSql + " NOT LIKE " + lp);
						else
							where.Add(colSql + " LIKE " + lp);
						break;
					case FilterOperation.In:
					case FilterOperation.NotIn:
						if (f.Values == null || f.Values.Count < 1) break;
						var vals = f.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
						if (vals.Count == 0) break;
						var inParams = new List<string>();
						foreach (var s in vals)
						{
							string ip = "@p" + (p++);
							parameters.Add(new SqlParameter(ip, ConvertValue(col.Type, s) ?? DBNull.Value));
							inParams.Add(ip);
						}
						where.Add(colSql + (f.Operation == FilterOperation.NotIn ? " NOT IN (" : " IN (") + string.Join(",", inParams) + ")");
						break;
				}
			}

			if (where.Count > 0)
				sb.Append(" WHERE ").Append(string.Join(" AND ", where));

			return (sb.ToString(), parameters);
		}

		private static object? ConvertValue(ReportColumnType type, string raw)
		{
			if (raw == null) return null;
			raw = raw.Trim();

			switch (type)
			{
				case ReportColumnType.Int32:
					return int.Parse(raw, CultureInfo.InvariantCulture);
				case ReportColumnType.Int64:
					return long.Parse(raw, CultureInfo.InvariantCulture);
				case ReportColumnType.Decimal:
					return decimal.Parse(raw, CultureInfo.InvariantCulture);
				case ReportColumnType.Double:
					return double.Parse(raw, CultureInfo.InvariantCulture);
				case ReportColumnType.Bool:
					return bool.Parse(raw);
				case ReportColumnType.Guid:
					return Guid.Parse(raw);
				case ReportColumnType.Date:
				case ReportColumnType.DateTime:
					return DateTime.Parse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
				case ReportColumnType.String:
				default:
					return raw;
			}
		}
	}
}
