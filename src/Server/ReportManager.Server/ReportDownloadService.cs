using PdfSharp.Fonts;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using ReportManager.Server.ReportExporters;
using ReportManager.Server.Services;
using System.Data;

namespace ReportManager.Server
{
	public class ReportDownloadService : IReportDownloadService
	{
		static ReportDownloadService()
		{
			// Register code pages provider for PdfSharp to support more encodings
			// System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
			GlobalFontSettings.UseWindowsFontsUnderWindows = true;
		}

		public Stream DownloadReport(ReportDownloadRequestDto request)
		{
			var reportQuery = request.ReportQuery ?? throw new ArgumentNullException(nameof(request.ReportQuery));

            var manifest = new ReportService().GetReportManifest(reportQuery.ReportKey, Constants.DefaultLanguage);
			var data = new ReportService().QueryReportInternal(reportQuery);

			var hiddenColumns = manifest.Columns.Where(c => c.Hidden).ToList();
			var visibleColumns = manifest.Columns.Where(c => !c.Hidden).ToDictionary(x => x.Key);
			var table = data.Rows;

			// Remove hidden columns
			foreach (var column in hiddenColumns)
			{
				if (table.Columns.Contains(column.Key))
				{
					// Mark for removal
					table.Columns.Remove(column.Key);
				}
			}

			// Rename columns to their display names
			foreach (DataColumn column in table.Columns)
			{
				if (!visibleColumns.TryGetValue(column.ColumnName, out var c))
				{
					// leave as-is when no manifest info available
					continue;
				}

				// Use display name as column caption so export header can use it while keeping ColumnName unique key
				column.Caption = c.DisplayName ?? column.ColumnName;
			}

			switch (request.FileFormat)
			{
				case FileFormat.Csv:
					return new CsvExporter().Export(table);
				case FileFormat.Xlsx:
					return new XlsxExporter().Export(table);
				case FileFormat.Pdf:
					return new PdfExporter().Export(table);
				case FileFormat.Json:
					return new JsonExporter().Export(table);
				default:
					throw new NotImplementedException();
			}
		}
	}
}
