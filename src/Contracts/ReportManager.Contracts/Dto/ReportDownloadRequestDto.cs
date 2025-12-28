using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportDownloadRequestDto
	{
		[DataMember(Order = 1)] public required string ReportKey { get; set; }
		[DataMember(Order = 2)] public required QuerySpecDto Query { get; set; }
		[DataMember(Order = 3)] public FileFormat FileFormat { get; set; }
	}
}
