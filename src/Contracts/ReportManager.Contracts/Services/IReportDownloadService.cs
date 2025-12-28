using ReportManager.ApiContracts.Dto;
using System.IO;
using System.ServiceModel;

namespace ReportManager.ApiContracts.Services
{
	[ServiceContract]
	public interface IReportDownloadService
	{
		[OperationContract]
		Stream DownloadReport(ReportDownloadRequestDto request);
	}
}