using ClosedXML.Excel;
using System;
using System.Data;
using System.IO;

namespace ReportManager.Server.ReportExporters
{
	internal sealed class XlsxExporter : ReportExporterBase, IReportExporter
	{
		public Stream Export(DataTable table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var ms = new MemoryStream();

			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Report");

				// Write header using column captions (or fallback to column name)
				for (int c = 0; c < table.Columns.Count; c++)
				{
					var header = GetColumnName(table.Columns[c]);
					worksheet.Cell(1, c + 1).SetValue(header);
				}

				// Write rows
				for (int r = 0; r < table.Rows.Count; r++)
				{
					var row = table.Rows[r];
					for (int c = 0; c < table.Columns.Count; c++)
					{
						var obj = row[c];
						if (obj == DBNull.Value || obj == null)
						{
							worksheet.Cell(r + 2, c + 1).Clear();
						}
						else
						{
							// Set as string representation to avoid ClosedXML overload issues
							worksheet.Cell(r + 2, c + 1).Value = XLCellValue.FromObject(obj);
						}
					}
				}

				// Format header and adjust columns
				var headerRange = worksheet.Range(1, 1, 1, Math.Max(1, table.Columns.Count));
				headerRange.Style.Font.Bold = true;
				worksheet.Row(1).Style.Alignment.WrapText = false;
				worksheet.Columns(1, table.Columns.Count).AdjustToContents();

				workbook.SaveAs(ms);
			}

			ms.Position = 0;
			return ms;
		}
	}
}
