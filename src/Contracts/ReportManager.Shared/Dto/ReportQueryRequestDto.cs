using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class ReportQueryRequestDto
	{
		[DataMember] public required string ReportKey { get; set; }
		[DataMember] public required QuerySpecDto Query { get; set; }
		[DataMember] public string Culture { get; set; } = Constants.DefaultLanguage;
        [DataMember] public int PageIndex { get; set; }
		[DataMember] public int? PageSize { get; set; }
	}
}
