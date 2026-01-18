namespace ReportAdmin.App.Messages
{
    public sealed class CultureChangedMessage
    {
        public string OldCulture { get; init; } = string.Empty;
        public string NewCulture { get; init; } = string.Empty;
    }
}
