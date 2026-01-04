namespace ReportAdmin.App.Messages
{
    public sealed class ReportColumnKeyChangedMessage
    {
        // Could be null when a new column is added
        public string? OldName { get; init; }
        // Could be null when a column is removed
        public string? NewName { get; init; }
    }
}
