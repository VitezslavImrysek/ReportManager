namespace ReportManager.Server.Utils
{
	internal static class SqlUtil
	{
		public static string QuoteIdentifier(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Empty identifier.");
			return "[" + name.Replace("]", "]]") + "]";
		}
	}
}
