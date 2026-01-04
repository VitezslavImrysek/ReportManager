using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportManager.Server.Services.ReportExporters
{
	internal sealed class CsvExporter : ReportExporterBase, IReportExporter
	{
		public Stream Export(DataTable table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var ms = new MemoryStream();

			// Use UTF8 with BOM for compatibility with Excel
			var utf8WithBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

			// leave the memory stream open after disposing writer
			using (var sw = new StreamWriter(ms, utf8WithBom, 1024, leaveOpen: true))
			{
				// write header
				for (int i = 0; i < table.Columns.Count; i++)
				{
					if (i > 0)
						sw.Write(';'); // use semicolon as delimiter (common in locales using comma decimal separator)

					var header = GetColumnName(table.Columns[i]);
					sw.Write(EscapeCsv(header));
				}
				sw.Write("\r\n");

				// write rows
				foreach (DataRow row in table.Rows)
				{
					for (int i = 0; i < table.Columns.Count; i++)
					{
						if (i > 0)
							sw.Write(';');

						var obj = row[i];
						var value = obj == null || obj == DBNull.Value ? string.Empty : Convert.ToString(obj);
						sw.Write(EscapeCsv(value));
					}
					sw.Write("\r\n");
				}

				sw.Flush();
			}

			ms.Position = 0;
			return ms;
		}

		private static string EscapeCsv(string input)
		{
			if (string.IsNullOrEmpty(input))
				return string.Empty;

			// If contains quote, delimiter or newline, wrap with quotes and escape quotes by doubling
			var needsQuotes = input.IndexOfAny(new char[] { '"', ';', '\r', '\n' }) >= 0;
			var escaped = input.Replace("\"", "\"\"");
			return needsQuotes ? '"' + escaped + '"' : escaped;
		}
	}
}
