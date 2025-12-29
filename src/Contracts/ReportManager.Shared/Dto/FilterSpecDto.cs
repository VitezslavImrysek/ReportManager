using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class FilterSpecDto
	{
		[DataMember(Order = 1)] public string ColumnKey { get; set; }
		[DataMember(Order = 2)] public FilterOperation Operation { get; set; }
		[DataMember(Order = 3)] public List<string> Values { get; set; } // Values as strings; server converts based on column type

		public FilterSpecDto()
		{
			ColumnKey = string.Empty;
			Values = new List<string>();
		}
	}
}
