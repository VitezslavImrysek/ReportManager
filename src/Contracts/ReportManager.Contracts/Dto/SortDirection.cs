using System.Runtime.Serialization;

namespace ReportManager.ApiContracts.Dto
{
    [DataContract]
    public enum SortDirection
    {
        [EnumMember] Asc = 0,
        [EnumMember] Desc = 1
    }
}
