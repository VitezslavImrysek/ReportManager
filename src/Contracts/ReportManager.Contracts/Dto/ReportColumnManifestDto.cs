using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportColumnManifestDto
	{
		[DataMember(Order = 1)] public required string Key { get; set; }
		[DataMember(Order = 2)] public required string DisplayName { get; set; }
		[DataMember(Order = 3)] public ReportColumnType Type { get; set; }
		[DataMember(Order = 4)] public bool Hidden { get; set; }
		[DataMember(Order = 5)] public bool AlwaysSelect { get; set; }
		[DataMember(Order = 6)] public bool FilterEnabled { get; set; }
		[DataMember(Order = 7)] public required List<FilterOperation> FilterOps { get; set; }
		[DataMember(Order = 8)] public bool SortEnabled { get; set; }
		[DataMember(Order = 9)] public LookupDto? Lookup { get; set; } 
	}
}
