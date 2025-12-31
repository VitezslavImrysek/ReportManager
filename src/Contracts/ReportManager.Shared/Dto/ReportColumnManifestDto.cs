using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public sealed class ReportColumnManifestDto
	{
		[DataMember] public required string Key { get; set; }
		[DataMember] public required string DisplayName { get; set; }
		[DataMember] public ReportColumnType Type { get; set; }
		[DataMember] public bool Hidden { get; set; }
		[DataMember] public bool AlwaysSelect { get; set; }
		[DataMember] public bool FilterEnabled { get; set; }
		[DataMember] public bool PrimaryKey { get; set; }
        [DataMember] public required List<FilterOperation> FilterOps { get; set; }
		[DataMember] public bool SortEnabled { get; set; }
		[DataMember] public LookupDto? Lookup { get; set; } 
	}
}
