using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
	[DataContract]
	public sealed class LookupItemDto
	{
		[DataMember(Order = 1)] public string Key { get; set; }   // always string for simplicity
		[DataMember(Order = 2)] public string Text { get; set; }

		public LookupItemDto()
		{
			Key = string.Empty;
			Text = string.Empty;
		}
	}
}
