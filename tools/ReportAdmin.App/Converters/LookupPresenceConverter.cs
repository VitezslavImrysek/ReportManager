using System.Globalization;
using System.Windows.Data;

namespace ReportAdmin.App.Converters
{
	public sealed class LookupPresenceConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			=> value != null;

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			=> Binding.DoNothing; // row-level edit řešeme přes UI v Lookup tabu
	}
}
