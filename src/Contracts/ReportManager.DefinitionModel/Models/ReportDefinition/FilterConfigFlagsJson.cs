namespace ReportManager.DefinitionModel.Models.ReportDefinition
{
    [Flags]
    public enum FilterConfigFlagsJson
    {
        None = 0,
        Hidden = 1 << 0,
    }
}
