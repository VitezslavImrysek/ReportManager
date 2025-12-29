using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{

	[DataContract]
	public sealed class SavePresetRequestDto
	{
		[DataMember(Order = 1)] public PresetDto Preset { get; set; }
		[DataMember(Order = 2)] public Guid UserId { get; set; } // for demo; in real app from auth context

		public SavePresetRequestDto()
		{
			Preset = new PresetDto();
		}
	}
}
