using ReportAdmin.App.ViewModels;

namespace ReportAdmin.App.Dialogs
{
	public class ImportDialogViewModel : NotificationObject
	{
		public required string ConnStringText { get; set => SetValue(ref field, value); }
		public required string SchemaText { get; set => SetValue(ref field, value); }
		public required string ViewText { get; set => SetValue(ref field, value); }
	}
}
