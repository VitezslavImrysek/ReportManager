#if Server && NET
using Microsoft.AspNetCore.Mvc;
using ReportManager.Server.Services;
using ReportManager.Shared.Dto;

namespace ReportManager.Server.Controllers
{
    // ASP.NET Core controller wrapper for the same functionality when compiling for .NET
    [ApiController]
    [Route("api/[controller]")]
    public class ReportDownloadController : ControllerBase
    {
        private readonly ReportDownloadService _service;

        public ReportDownloadController()
        {
            _service = new ReportDownloadService();
        }

        [HttpPost("download")]
        public IActionResult Download([FromBody] ReportDownloadRequestDto request)
        {
            if (request == null) return BadRequest();

            var stream = _service.DownloadReport(request);
            if (stream == null) return NotFound();

            try
            {
                stream.Position = 0;
            }
            catch { }

            var fileName = request.ReportQuery?.ReportKey ?? "report";
            var (contentType, ext) = request.FileFormat switch
            {
                FileFormat.Csv => ("text/csv", "csv"),
                FileFormat.Xlsx => ("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "xlsx"),
                FileFormat.Pdf => ("application/pdf", "pdf"),
                FileFormat.Json => ("application/json", "json"),
                _ => ("application/octet-stream", "bin"),
            };

            return File(stream, contentType, $"{fileName}.{ext}");
        }
    }
}
#endif
