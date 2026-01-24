using ReportAdmin.App.ViewModels;

namespace ReportAdmin.App.Messages
{
    public sealed class GetColumnsMessage
    {
        public List<IColumn> Columns { get; } = [];
    }
}
