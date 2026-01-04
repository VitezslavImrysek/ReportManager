using ReportManager.Shared.Dto;


#if Server && NET
using CoreWCF;
#else
using System.IO;
using System.ServiceModel;
#endif

#if Server
namespace ReportManager.Server.Wcf
#else
namespace ReportManager.Proxy.Services
#endif
{
	[ServiceContract]
	public interface IReportDownloadService
	{
		[OperationContract]
		Stream DownloadReport(ReportDownloadRequestDto request);
	}
}