using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
	[DataContract]
	public enum FileFormat
	{
		[EnumMember]
		Csv = 1,
		[EnumMember]
		Xlsx = 2,
		[EnumMember]
		Pdf = 3,
		[EnumMember]
		Json = 4
	}
}
