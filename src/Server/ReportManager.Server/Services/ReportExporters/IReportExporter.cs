using System.Data;
using System.IO;

namespace ReportManager.Server.Services.ReportExporters
{
	internal interface IReportExporter
	{
		Stream Export(DataTable table);
	}
}
