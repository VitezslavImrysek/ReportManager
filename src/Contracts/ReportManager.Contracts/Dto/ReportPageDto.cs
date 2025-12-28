using System.Data;
using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportPageDto
	{
		[DataMember(Order = 1)] public DataTable Rows { get; set; }
		[DataMember(Order = 2)] public int TotalCount { get; set; }

		public ReportPageDto()
		{
			Rows = new DataTable("Rows");
		}
	}
}
