using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public enum FilterOperation
	{
		[EnumMember] Eq = 0,
		[EnumMember] Ne = 1,
		[EnumMember] Gt = 2,
		[EnumMember] Ge = 3,
		[EnumMember] Lt = 4,
		[EnumMember] Le = 5,
		[EnumMember] Between = 6,
		[EnumMember] Contains = 7,
		[EnumMember] NotContains = 8,
		[EnumMember] StartsWith = 9,
		[EnumMember] EndsWith = 10,
		[EnumMember] In = 11,
		[EnumMember] NotIn = 12,
		[EnumMember] IsNull = 13,
		[EnumMember] NotNull = 14
	}
}
