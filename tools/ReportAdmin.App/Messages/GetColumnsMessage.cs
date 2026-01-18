using ReportAdmin.Core.Models.Definition;

namespace ReportAdmin.App.Messages
{
    public sealed class GetColumnsMessage
    {
        public List<ReportColumnUi> Columns { get; init; } = new();
    }
}
