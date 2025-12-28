using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReportAdmin.App.ViewModels;

public abstract class NotificationObject : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected void OnPropertyChanged([CallerMemberName] string? name = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

	protected bool SetValue<T>(ref T field, T value, [CallerMemberName] string? name = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value)) return false;
		field = value;
		OnPropertyChanged(name);
		return true;
	}
}
