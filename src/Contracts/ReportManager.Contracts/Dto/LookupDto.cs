using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class LookupDto
	{
		[DataMember(Order = 1)] public List<LookupItemDto> Items { get; set; }

		public LookupDto()
		{
			Items = new List<LookupItemDto>();
		}
	}
}
