using LinqToDB.Mapping;

namespace ReportManager.DefinitionModel.Models
{
    [Table(Schema = "dbo", Name = "ReportDefinition")]
    public class ReportDefinitionDb
    {
        [PrimaryKey, Identity]
        public int ReportDefinitionId { get; set; }
        [Column, NotNull]
        public string Key { get; set; } = string.Empty;
        [Column, NotNull]
        public string ViewSchema { get; set; } = string.Empty;
        [Column, NotNull]
        public string ViewName { get; set; } = string.Empty;
        [Column, NotNull]
        public string DefinitionJson { get; set; } = string.Empty;
        [Column, NotNull]
        public bool IsActive { get; set; } = true;
        [Column, NotNull]
        public DateTimeOffset UpdatedUtc { get; set; }
    }
}
