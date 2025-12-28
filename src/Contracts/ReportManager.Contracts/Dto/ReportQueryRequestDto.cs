using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportQueryRequestDto
	{
		[DataMember(Order = 1)] public string ReportKey { get; set; }
		[DataMember(Order = 2)] public QuerySpecDto Query { get; set; }
		[DataMember(Order = 3)] public int PageIndex { get; set; }
		[DataMember(Order = 4)] public int PageSize { get; set; }

		public ReportQueryRequestDto()
		{
			ReportKey = "";
			Query = new QuerySpecDto();
			PageIndex = 0;
			PageSize = 100;
		}
	}
}
