using LinqToDB.Mapping;

namespace ReportManager.DefinitionModel.Models
{
    [Table(Schema = "dbo", Name = "ReportViewPreset")]
    public class ReportViewPresetDb
    {
        [PrimaryKey, Identity]
        public Guid PresetId { get; set; }
        [Column, NotNull]
        public string ReportKey { get; set; } = string.Empty;
        [Column, Nullable]
        public Guid? OwnerUserId { get; set; }
        [Column, NotNull]
        public string PresetJson { get; set; } = string.Empty;
        [Column, NotNull]
        public bool IsDefault { get; set; }
        [Column, NotNull]
        public DateTimeOffset CreatedUtc { get; set; }
        [Column, NotNull]
        public DateTimeOffset UpdatedUtc { get; set; }
    }
}
