using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportManifestDto
	{
		[DataMember(Order = 1)] public string ReportKey { get; set; }
		[DataMember(Order = 2)] public string Title { get; set; }
		[DataMember(Order = 3)] public string Culture { get; set; }
		[DataMember(Order = 4)] public int Version { get; set; }
		[DataMember(Order = 5)] public List<ReportColumnManifestDto> Columns { get; set; }
		[DataMember(Order = 6)] public List<SortSpecDto> DefaultSort { get; set; }

		public ReportManifestDto()
		{
			ReportKey = string.Empty;
			Title = string.Empty;
			Culture = string.Empty;
			Columns = [];
			DefaultSort = [];
		}
	}
}
