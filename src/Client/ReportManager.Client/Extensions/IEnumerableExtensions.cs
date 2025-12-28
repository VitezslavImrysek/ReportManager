using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ReportManager.Client.Extensions
{
	public static class IEnumerableExtensions
	{
		public static ObservableCollection<T> ToObservable<T>(this IEnumerable<T> source)
		{
			return new ObservableCollection<T>(source);
		}
	}
}
