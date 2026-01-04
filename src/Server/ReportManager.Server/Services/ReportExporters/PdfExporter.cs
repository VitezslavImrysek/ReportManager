using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Data;

namespace ReportManager.Server.Services.ReportExporters
{
	internal sealed class PdfExporter : ReportExporterBase, IReportExporter
	{
		public Stream Export(DataTable table)
		{
			if (table == null)
				throw new ArgumentNullException(nameof(table));

			var document = new PdfDocument();
			document.Info.Title = "DataTable Export";

			PdfPage page = document.AddPage();
			XGraphics gfx = XGraphics.FromPdfPage(page);

			var fontHeader = new XFont("Arial", 10, XFontStyleEx.Bold);
			var fontCell = new XFont("Arial", 9, XFontStyleEx.Regular);

			double margin = 40;
			double y = margin;
			double rowHeight = 18;

			double tableWidth = page.Width - 2 * margin;
			double columnWidth = tableWidth / table.Columns.Count;

			// --- HEADER ---
			for (int i = 0; i < table.Columns.Count; i++)
			{
				gfx.DrawRectangle(XPens.Black, margin + i * columnWidth, y, columnWidth, rowHeight);
				gfx.DrawString(
					GetColumnName(table.Columns[i]),
					fontHeader,
					XBrushes.Black,
					new XRect(margin + i * columnWidth + 3, y + 3, columnWidth - 6, rowHeight),
					XStringFormats.CenterLeft
				);
			}

			y += rowHeight;

			// --- ROWS ---
			foreach (DataRow row in table.Rows)
			{
				// Zalomení stránky
				if (y + rowHeight > page.Height - margin)
				{
					page = document.AddPage();
					gfx = XGraphics.FromPdfPage(page);
					y = margin;
				}

				for (int i = 0; i < table.Columns.Count; i++)
				{
					gfx.DrawRectangle(XPens.Black, margin + i * columnWidth, y, columnWidth, rowHeight);

					string text = row[i]?.ToString() ?? string.Empty;

					gfx.DrawString(
						text,
						fontCell,
						XBrushes.Black,
						new XRect(margin + i * columnWidth + 3, y + 3, columnWidth - 6, rowHeight),
						XStringFormats.CenterLeft
					);
				}

				y += rowHeight;
			}

			// --- OUTPUT ---
			var stream = new MemoryStream();
			document.Save(stream, false);
			stream.Position = 0;

			return stream;
		}

	}
}
