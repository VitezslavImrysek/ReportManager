using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
    [DataContract]
    public sealed class SetDefaultPresetRequestDto
    {
        [DataMember]
        public Guid PresetId { get; set; }
        [DataMember]
        public required string ReportKey { get; set; }
        [DataMember]
        public Guid UserId { get; set; }
    }
}
