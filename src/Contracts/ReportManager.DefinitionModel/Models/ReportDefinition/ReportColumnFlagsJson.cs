namespace ReportManager.DefinitionModel.Models.ReportDefinition
{
    [Flags]
    public enum ReportColumnFlagsJson
    {
        None = 0,
        AlwaysSelect = 1 << 0,
        Hidden = 1 << 1,
        PrimaryKey = 1 << 2,
    }
}
