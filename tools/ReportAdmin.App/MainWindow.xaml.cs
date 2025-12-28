using System.Windows;

namespace ReportAdmin.App;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();
		DataContext = new ViewModels.MainViewModel();
	}
}
