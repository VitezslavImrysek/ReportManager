using ReportAdmin.App.ViewModels;

namespace ReportAdmin.App.Messages
{
    public class ResolveTextKeyMessage
    {
        public required TextsEditorMode TextsEditorMode { get; init; }
        public required string Culture { get; init; }
        public required string Key { get; init; }
        public string? Value { get; set; }
    }
}
