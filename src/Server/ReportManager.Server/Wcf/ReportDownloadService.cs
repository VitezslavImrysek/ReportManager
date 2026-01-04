using ReportManager.Shared.Dto;

namespace ReportManager.Server.Wcf
{
    public sealed class ReportDownloadService : IReportDownloadService
    {
        public Stream DownloadReport(ReportDownloadRequestDto request)
        {
            return new Services.ReportDownloadService().DownloadReport(request);
        }
    }
}
