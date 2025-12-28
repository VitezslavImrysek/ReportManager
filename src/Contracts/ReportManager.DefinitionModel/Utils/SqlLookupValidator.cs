using Microsoft.SqlServer.TransactSql.ScriptDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReportManager.DefinitionModel.Utils
{
	public static class SqlLookupValidator
	{
		public static bool TryValidate(string? sql, out string error)
		{
			if (string.IsNullOrWhiteSpace(sql))
			{
				error = "Lookup SQL is empty.";
				return false;
			}

			var parser = new TSql160Parser(initialQuotedIdentifiers: true);
			IList<ParseError> errors;
			var statements = parser.ParseStatementList(new StringReader(sql), out errors);

			if (errors is { Count: > 0 })
			{
				error = string.Join("; ", errors.Select(e => $"{e.Line},{e.Column}: {e.Message}"));
				return false;
			}

			if (statements == null || statements.Count != 1)
			{
				error = "Lookup SQL must contain exactly one SELECT statement.";
				return false;
			}

			if (statements[0] is not SelectStatement select)
			{
				error = "Lookup SQL must be a SELECT statement.";
				return false;
			}

			if (select.Into != null)
			{
				error = "SELECT INTO is not allowed in lookup SQL.";
				return false;
			}

			if (select.QueryExpression == null)
			{
				error = "Lookup SQL is missing a query expression.";
				return false;
			}

			error = string.Empty;
			return true;
		}
	}
}
