using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class PresetInfoDto
	{
		[DataMember] public Guid PresetId { get; set; }
		[DataMember] public string Name { get; set; }
		[DataMember] public bool IsSystem { get; set; }
		[DataMember] public bool IsDefault { get; set; }

		public PresetInfoDto()
		{
			Name = string.Empty;
		}
	}
}
