using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class ReportManifestDto
	{
		[DataMember] public string ReportKey { get; set; }
		[DataMember] public string Title { get; set; }
		[DataMember] public List<ReportColumnManifestDto> Columns { get; set; }
		[DataMember] public List<SortSpecDto> DefaultSort { get; set; }

		public ReportManifestDto()
		{
			ReportKey = string.Empty;
			Title = string.Empty;
			Columns = [];
			DefaultSort = [];
		}
	}
}
