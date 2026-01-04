using ReportManager.Shared.Dto;

namespace ReportManager.Server.Wcf
{
    public sealed class ReportService : IReportService
    {
        public void DeletePreset(Guid presetId, Guid userId)
        {
            new Services.ReportService().DeletePreset(presetId, userId);
        }

        public PresetDto GetPreset(Guid presetId, Guid userId)
        {
            return new Services.ReportService().GetPreset(presetId, userId);
        }

        public List<PresetInfoDto> GetPresets(string reportKey, Guid userId)
        {
            return new Services.ReportService().GetPresets(reportKey, userId);
        }

        public ReportManifestDto GetReportManifest(string reportKey)
        {
            return new Services.ReportService().GetReportManifest(reportKey);
        }

        public ReportPageDto QueryReport(ReportQueryRequestDto request)
        {
            return new Services.ReportService().QueryReport(request);
        }

        public Guid SavePreset(SavePresetRequestDto request)
        {
            return new Services.ReportService().SavePreset(request);
        }

        public void SetDefaultPreset(Guid presetId, string reportKey, Guid userId)
        {
            new Services.ReportService().SetDefaultPreset(presetId, reportKey, userId);
        }
    }
}
