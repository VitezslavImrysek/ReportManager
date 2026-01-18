using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class GridStateDto
	{
		[DataMember] public List<string> HiddenColumns { get; set; }
		[DataMember] public List<string> Order { get; set; } 

		public GridStateDto()
		{
			HiddenColumns = [];
			Order = [];
		}
	}
}
