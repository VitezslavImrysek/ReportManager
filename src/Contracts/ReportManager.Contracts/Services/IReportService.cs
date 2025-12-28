using ReportManager.ApiContracts.Dto;
using System.ServiceModel;

namespace ReportManager.ApiContracts.Services
{
	[ServiceContract]
	public interface IReportService
	{
		[OperationContract]
		ReportManifestDto GetReportManifest(string reportKey, string culture);

		[OperationContract]
		ReportPageDto QueryReport(ReportQueryRequestDto request);

		[OperationContract]
		List<PresetInfoDto> GetPresets(string reportKey, Guid userId);

		[OperationContract]
		PresetDto GetPreset(Guid presetId, Guid userId);

		[OperationContract]
		Guid SavePreset(SavePresetRequestDto request);

		[OperationContract]
		void DeletePreset(Guid presetId, Guid userId);

		[OperationContract]
		void SetDefaultPreset(Guid presetId, string reportKey, Guid userId);
	}
}