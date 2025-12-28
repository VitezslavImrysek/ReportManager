using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public enum ReportColumnType
	{
		[EnumMember] Int32 = 0,
		[EnumMember] Int64 = 1,
		[EnumMember] Decimal = 2,
		[EnumMember] Double = 3,
		[EnumMember] String = 4,
		[EnumMember] DateTime = 5,
		[EnumMember] Date = 6,
		[EnumMember] Bool = 7,
		[EnumMember] Guid = 8
	}
}
