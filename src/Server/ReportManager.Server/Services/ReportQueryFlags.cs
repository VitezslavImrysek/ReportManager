namespace ReportManager.Server.Services
{
    [Flags]
    internal enum ReportQueryFlags
    {
        None = 0,
        SelectPrimaryKeyOnly = 1 << 0
    }
}
