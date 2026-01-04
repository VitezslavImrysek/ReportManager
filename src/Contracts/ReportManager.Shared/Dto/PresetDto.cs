using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class PresetDto
	{
		[DataMember] public Guid PresetId { get; set; }
		[DataMember] public string ReportKey { get; set; }
		[DataMember] public string Name { get; set; }
		[DataMember] public bool IsSystem { get; set; }
		[DataMember] public bool IsDefault { get; set; }
		[DataMember] public PresetContentDto Content { get; set; }

		public PresetDto()
		{
			ReportKey = string.Empty;
			Name = string.Empty;
			Content = new PresetContentDto();
		}
	}
}
