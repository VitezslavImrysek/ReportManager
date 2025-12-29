using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportQueryRequestDto
	{
		[DataMember(Order = 1)] public required string ReportKey { get; set; }
		[DataMember(Order = 2)] public required QuerySpecDto Query { get; set; }
		[DataMember(Order = 3)] public int PageIndex { get; set; }
		[DataMember(Order = 4)] public int? PageSize { get; set; }
	}
}
