using System.Windows;

namespace ReportAdmin.App.Dialogs;

public partial class ImportDialog : Window
{
	public ImportDialog()
	{
		InitializeComponent();
	}

	private void Ok_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = true;
		Close();
	}

	private void Cancel_Click(object sender, RoutedEventArgs e)
	{
		DialogResult = false;
		Close();
	}
}
