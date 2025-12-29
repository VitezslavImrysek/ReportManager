namespace ReportManager.Host
{
	internal static class Program
	{
        static void Main(string[] args)
        {
#if NETFRAMEWORK
            new NetFxHost().Run(args);
#elif NET
            new NetHost().Run(args);
#endif
        }
    }
}
