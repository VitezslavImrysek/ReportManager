using System.Data;
using System.IO;

namespace ReportManager.Server.ReportExporters
{
	internal interface IReportExporter
	{
		Stream Export(DataTable table);
	}
}
