using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class QuerySpecDto
	{
		[DataMember] public List<FilterSpecDto> Filters { get; set; }
		[DataMember] public List<SortSpecDto> Sorting { get; set; }
		[DataMember] public List<string> SelectedColumns { get; set; } // optional; empty => server chooses (visible+alwaysSelect)

		public QuerySpecDto()
		{
			Filters = new List<FilterSpecDto>();
			Sorting = new List<SortSpecDto>();
			SelectedColumns = new List<string>();
		}
	}
}
