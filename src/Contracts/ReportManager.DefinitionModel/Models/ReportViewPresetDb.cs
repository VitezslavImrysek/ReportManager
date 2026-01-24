using LinqToDB.Mapping;

namespace ReportManager.DefinitionModel.Models
{
    [Table(Schema = "dbo", Name = "ReportViewPreset")]
    public class ReportViewPresetDb
    {
        [PrimaryKey]
        public Guid PresetId { get; set; }
        [Column, NotNull]
        public int ReportDefinitionId { get; set; }
        [Column, Nullable]
        public Guid? OwnerUserId { get; set; }
        [Column, NotNull]
        public string PresetJson { get; set; } = string.Empty;
        [Column, NotNull]
        public bool IsDefault { get; set; }
    }
}
