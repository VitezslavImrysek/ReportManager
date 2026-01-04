#if Server && NET
using Microsoft.AspNetCore.Mvc;
using ReportManager.Server.Services;
using ReportManager.Shared.Dto;

namespace ReportManager.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportController : ControllerBase
    {
        private readonly ReportService _service;

        public ReportController(ReportService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        [HttpGet("manifest")]
        public ActionResult<ReportManifestDto> GetReportManifest([FromQuery] string reportKey)
            => Ok(_service.GetReportManifest(reportKey));

        [HttpPost("query")]
        public ActionResult<ReportPageDto> QueryReport([FromBody] ReportQueryRequestDto request)
            => Ok(_service.QueryReport(request));

        [HttpGet("presets")]
        public ActionResult<List<PresetInfoDto>> GetPresets([FromQuery] string reportKey, [FromQuery] Guid userId)
            => Ok(_service.GetPresets(reportKey, userId));

        [HttpGet("preset")]
        public ActionResult<PresetDto> GetPreset([FromQuery] Guid presetId, [FromQuery] Guid userId)
            => Ok(_service.GetPreset(presetId, userId));

        [HttpPost("preset")]
        public ActionResult<Guid> SavePreset([FromBody] SavePresetRequestDto request)
        {
            var id = _service.SavePreset(request);
            return Ok(id);
        }

        [HttpDelete("preset")]
        public IActionResult DeletePreset([FromQuery] Guid presetId, [FromQuery] Guid userId)
        {
            _service.DeletePreset(presetId, userId);
            return NoContent();
        }

        [HttpPost("preset/default")]
        public IActionResult SetDefaultPreset([FromBody] SetDefaultPresetRequestDto request)
        {
            _service.SetDefaultPreset(request.PresetId, request.ReportKey, request.UserId);
            return NoContent();
        }
    }
}
#endif