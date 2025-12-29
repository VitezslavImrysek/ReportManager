using System.Runtime.Serialization;

namespace ReportManager.Shared.Dto
{
    [DataContract]
    public enum SortDirection
    {
        [EnumMember] Asc = 0,
        [EnumMember] Desc = 1
    }
}
