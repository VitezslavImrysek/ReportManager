using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class PresetContentDto
	{
		[DataMember] public GridStateDto Grid { get; set; }
		[DataMember] public QuerySpecDto Query { get; set; }

		public PresetContentDto()
		{
			Grid = new GridStateDto();
			Query = new QuerySpecDto();
		}
	}
}
