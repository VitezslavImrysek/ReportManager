using Newtonsoft.Json;
using PdfSharp.Fonts;
using ReportManager.Server.Services.ReportExporters;
using ReportManager.Shared.Dto;
using System.Data;
using System.Text;

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
			var data = new ReportService().QueryReportInternal(reportQuery, ReportQueryFlags.None);

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

        public Stream DownloadPrimaryKeyList(ReportQueryRequestDto request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var query = request.Query ?? new QuerySpecDto();
            var pkQuery = new ReportQueryRequestDto
            {
                ReportKey = request.ReportKey,
                Query = new QuerySpecDto
                {
                    Filters = query.Filters ?? [],
                    Sorting = query.Sorting ?? [],
                    SelectedColumns = []
                },
                PageIndex = 0,
                PageSize = null
            };

            // QueryReportInternal with SelectPrimaryKeyOnly returns only the PK column at index 0
			// (there are no other columns)
            const int PrimaryKeyColumnIndex = 0; 

            var reportService = new ReportService();
            // Execute query but get only primary key column - skip all alwaysAdd columns
            var data = reportService.QueryReportInternal(pkQuery, ReportQueryFlags.SelectPrimaryKeyOnly);
            var values = data.Rows.Rows
                .Cast<DataRow>()
                .Select(row => row[PrimaryKeyColumnIndex] == DBNull.Value ? null : row[PrimaryKeyColumnIndex])
                .ToList();

            var json = JsonConvert.SerializeObject(values, Formatting.None);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            stream.Position = 0;

            return stream;
        }
    }
}
