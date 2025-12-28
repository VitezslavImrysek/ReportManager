#nullable enable

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace ReportManager.Server.ReportExporters
{
	internal sealed class JsonExporter : ReportExporterBase, IReportExporter
	{
		public Stream Export(DataTable table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var exportObject = new
			{
				columns = BuildColumns(table),
				rows = BuildRows(table)
			};

			var json = JsonConvert.SerializeObject(exportObject, Formatting.Indented);

			var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
			stream.Position = 0;
			return stream;
		}

		private IEnumerable<object> BuildColumns(DataTable table)
		{
			foreach (DataColumn column in table.Columns)
			{
				yield return new
				{
					name = column.ColumnName,
					caption = GetColumnName(column),
					dataType = column.DataType.Name
				};
			}
		}

		private IEnumerable<IDictionary<string, object?>> BuildRows(DataTable table)
		{
			foreach (DataRow row in table.Rows)
			{
				var dict = new Dictionary<string, object?>();

				foreach (DataColumn column in table.Columns)
				{
					dict[column.ColumnName] =
						row[column] == DBNull.Value ? null : row[column];
				}

				yield return dict;
			}
		}
	}
}
