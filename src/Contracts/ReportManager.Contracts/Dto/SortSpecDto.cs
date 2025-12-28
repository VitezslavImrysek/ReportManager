using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class SortSpecDto
	{
		[DataMember(Order = 1)] public string ColumnKey { get; set; }
		[DataMember(Order = 2)] public SortDirection Direction { get; set; }

		public SortSpecDto()
		{
			ColumnKey = string.Empty;
			Direction = SortDirection.Asc;
		}
	}
}
