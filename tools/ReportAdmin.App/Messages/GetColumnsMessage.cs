namespace ReportAdmin.App.Messages
{
    public sealed class GetColumnsMessage
    {
        public List<string> ColumnNames { get; init; } = new();
    }
}
