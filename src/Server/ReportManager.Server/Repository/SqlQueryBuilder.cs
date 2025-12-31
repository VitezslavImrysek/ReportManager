using ReportManager.Shared.Dto;
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
			int? pageSize)
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

				// Basic whitelist of operations by type is assumed done earlier; here just implement a subset.
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
						if (!TryConvertValue(col.Type, f.Values[0], out var val)) break;
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
						if (!TryConvertValue(col.Type, f.Values[0], out var v1)) break;
						if (!TryConvertValue(col.Type, f.Values[1], out var v2)) break;
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
						// TODO: For now using parameters list. Possible improvement is to switch to TVP for larger lists.
						var vals = f.Values.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
						if (vals.Count == 0) break;
						var inParams = new List<string>();
						foreach (var s in vals)
						{
							if (!TryConvertValue(col.Type, s, out var inVal)) continue;
							string ip = "@p" + (p++);
							parameters.Add(new SqlParameter(ip, inVal ?? DBNull.Value));
							inParams.Add(ip);
						}
						if (inParams.Count == 0) break;
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

			if (pageSize != null)
			{
                sb.Append(" OFFSET ").Append(pageIndex * pageSize).Append(" ROWS FETCH NEXT ").Append(pageSize).Append(" ROWS ONLY;");
            }

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
						if (!TryConvertValue(col.Type, f.Values[0], out var val)) break;
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
						if (!TryConvertValue(col.Type, f.Values[0], out var v1)) break;
						if (!TryConvertValue(col.Type, f.Values[1], out var v2)) break;
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
							if (!TryConvertValue(col.Type, s, out var inVal)) continue;
							string ip = "@p" + (p++);
							parameters.Add(new SqlParameter(ip, inVal ?? DBNull.Value));
							inParams.Add(ip);
						}
						if (inParams.Count == 0) break;
						where.Add(colSql + (f.Operation == FilterOperation.NotIn ? " NOT IN (" : " IN (") + string.Join(",", inParams) + ")");
						break;
				}
			}

			if (where.Count > 0)
				sb.Append(" WHERE ").Append(string.Join(" AND ", where));

			return (sb.ToString(), parameters);
		}

		private static bool TryConvertValue(ReportColumnType type, string raw, out object? value)
		{
			value = null;
			if (raw == null) return false;
			raw = raw.Trim();

			switch (type)
			{
				case ReportColumnType.Integer:
					if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i32)) return false;
					value = i32;
					return true;
				case ReportColumnType.Long:
					if (!long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i64)) return false;
					value = i64;
					return true;
				case ReportColumnType.Decimal:
					if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var dec)) return false;
					value = dec;
					return true;
				case ReportColumnType.Double:
					if (!double.TryParse(raw, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dbl)) return false;
					value = dbl;
					return true;
				case ReportColumnType.Boolean:
					if (!bool.TryParse(raw, out var b)) return false;
					value = b;
					return true;
				case ReportColumnType.Guid:
					if (!Guid.TryParse(raw, out var g)) return false;
					value = g;
					return true;
				case ReportColumnType.Date:
				case ReportColumnType.DateTime:
					if (!DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt)) return false;
					value = dt;
					return true;
				case ReportColumnType.String:
				default:
					value = raw;
					return true;
			}
		}
	}
}
