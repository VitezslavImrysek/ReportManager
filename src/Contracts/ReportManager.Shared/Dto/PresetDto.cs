using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class PresetDto
	{
		[DataMember(Order = 1)] public Guid PresetId { get; set; }
		[DataMember(Order = 2)] public string ReportKey { get; set; }
		[DataMember(Order = 3)] public string Name { get; set; }
		[DataMember(Order = 4)] public bool IsSystem { get; set; }
		[DataMember(Order = 5)] public bool IsDefault { get; set; }

		[DataMember(Order = 6)] public PresetContentDto Content { get; set; }

		public PresetDto()
		{
			ReportKey = string.Empty;
			Name = string.Empty;
			Content = new PresetContentDto();
		}
	}
}
