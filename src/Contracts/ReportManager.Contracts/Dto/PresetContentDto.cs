using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class PresetContentDto
	{
		[DataMember(Order = 1)] public int Version { get; set; }
		[DataMember(Order = 2)] public GridStateDto Grid { get; set; }
		[DataMember(Order = 3)] public QuerySpecDto Query { get; set; }

		public PresetContentDto()
		{
			Version = 1;
			Grid = new GridStateDto();
			Query = new QuerySpecDto();
		}
	}
}
