using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{

	[DataContract]
	public enum LookupMode
	{
		[EnumMember] Static = 0,
		[EnumMember] Sql = 1,
	}
}
