using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class ReportDownloadRequestDto
	{
		[DataMember(Order = 1)] public required ReportQueryRequestDto ReportQuery { get; set; }
		[DataMember(Order = 2)] public FileFormat FileFormat { get; set; }
	}
}
