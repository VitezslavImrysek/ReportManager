using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class PresetInfoDto
	{
		[DataMember(Order = 1)] public Guid PresetId { get; set; }
		[DataMember(Order = 2)] public string Name { get; set; }
		[DataMember(Order = 3)] public bool IsSystem { get; set; }
		[DataMember(Order = 4)] public bool IsDefault { get; set; }

		public PresetInfoDto()
		{
			Name = string.Empty;
		}
	}
}
