using System;

namespace ReportManager.Server.ReportExporters
{
	internal abstract class ReportExporterBase
	{
		protected string GetColumnName(System.Data.DataColumn column)
		{
			if (column == null)
				throw new ArgumentNullException(nameof(column));
			return string.IsNullOrWhiteSpace(column.Caption) ? column.ColumnName : column.Caption;
		}
	}
}
