using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class QuerySpecDto
	{
		[DataMember(Order = 1)] public List<FilterSpecDto> Filters { get; set; }
		[DataMember(Order = 2)] public List<SortSpecDto> Sorting { get; set; }
		[DataMember(Order = 3)] public List<string> SelectedColumns { get; set; } // optional; empty => server chooses (visible+alwaysSelect)

		public QuerySpecDto()
		{
			Filters = new List<FilterSpecDto>();
			Sorting = new List<SortSpecDto>();
			SelectedColumns = new List<string>();
		}
	}
}
