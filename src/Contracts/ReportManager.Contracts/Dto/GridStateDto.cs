using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class GridStateDto
	{
		[DataMember(Order = 1)] public List<string> HiddenColumns { get; set; }
		[DataMember(Order = 2)] public List<string> Order { get; set; } // optional

		public GridStateDto()
		{
			HiddenColumns = new List<string>();
			Order = new List<string>();
		}
	}
}
