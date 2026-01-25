using PdfSharp.Fonts;
using ReportManager.Server.Services.ReportExporters;
using ReportManager.Shared;
using ReportManager.Shared.Dto;
using System.Data;

namespace ReportManager.Server.Services
{
	public class ReportDownloadService
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

            var manifest = new ReportService().GetReportManifest(reportQuery.ReportKey);
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

		public async Task<Stream> DownloadPrimaryKeyList(ReportDownloadRequestDto request)
		{
			var reportQuery = request.ReportQuery ?? throw new ArgumentNullException(nameof(request.ReportQuery));

			// Get manifest to identify primary key column
			var manifest = new ReportService().GetReportManifest(reportQuery.ReportKey);
			var primaryKeyColumn = manifest.Columns.FirstOrDefault(c => c.PrimaryKey);

			if (primaryKeyColumn == null)
			{
				throw new InvalidOperationException($"Report '{reportQuery.ReportKey}' does not have a primary key column defined.");
			}

			// Query data without page size limitations
			var data = new ReportService().QueryReportInternal(reportQuery);
			var table = data.Rows;

			// Extract primary key values
			if (!table.Columns.Contains(primaryKeyColumn.Key))
			{
				throw new InvalidOperationException($"Primary key column '{primaryKeyColumn.Key}' was not found in the query results.");
			}

			var primaryKeys = new List<int>();
			foreach (DataRow row in table.Rows)
			{
				var value = row[primaryKeyColumn.Key];
				if (value != DBNull.Value)
				{
					primaryKeys.Add(Convert.ToInt32(value));
				}
			}

			// Serialize to JSON and return as stream
			return await SerializeToJsonStreamAsync(primaryKeys.ToArray());
		}

		private async Task<Stream> SerializeToJsonStreamAsync(int[] primaryKeys)
		{
			var stream = new MemoryStream();
			var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, leaveOpen: true);

			try
			{
				// Serialize using Newtonsoft.Json for consistency with existing exporters
				var json = Newtonsoft.Json.JsonConvert.SerializeObject(primaryKeys);
				await writer.WriteAsync(json);
				await writer.FlushAsync();

				stream.Position = 0;
				return stream;
			}
			catch
			{
				writer.Dispose();
				stream.Dispose();
				throw;
			}
		}
	}
}
