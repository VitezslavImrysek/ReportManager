using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public enum ReportColumnType
	{
		[EnumMember] Integer = 0,
		[EnumMember] Long = 1,
		[EnumMember] Decimal = 2,
		[EnumMember] Double = 3,
		[EnumMember] String = 4,
		[EnumMember] DateTime = 5,
		[EnumMember] Date = 6,
		[EnumMember] Boolean = 7,
		[EnumMember] Guid = 8
	}
}
