using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models.Definition;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ReportAdmin.App;

public partial class MainWindow : Window
{
    // Klíč: (row object, property name) → old value
    private readonly Dictionary<object, string> _oldValues = new();

    public MainWindow()
	{
		InitializeComponent();
		DataContext = new ViewModels.MainViewModel();
	}

    private void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
            return;

        var propName = GetBoundPropertyName(e.Column);
        if (propName != nameof(ReportColumnUi.Key)) return;

        var rowItem = e.Row.Item as ReportColumnUi;
        if (rowItem == null) return;

        _oldValues.TryGetValue(rowItem, out var oldValue);

        // newValue vezmeme z propertky (ta už může být aktualizovaná)
        var newValue = rowItem.Key;

        if (oldValue != newValue)
        {
            Messenger.Instance.Send(new ReportColumnKeyChangedMessage() { OldName = oldValue, NewName = newValue });
        }

        _oldValues.Remove((rowItem, propName));
    }

    private void dataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
    {
        var propName = GetBoundPropertyName(e.Column);
        if (propName != nameof(ReportColumnUi.Key)) return;

        var rowItem = e.Row.Item as ReportColumnUi;
        if (rowItem == null) return;

        var oldKey = rowItem.Key;

        _oldValues[rowItem] = oldKey;
    }

    private static string? GetBoundPropertyName(DataGridColumn col)
    {
        if (col is DataGridBoundColumn bc && bc.Binding is Binding b && b.Path != null)
            return b.Path.Path;

        return null; // pro templated/sloužitější bindingy je potřeba jiný přístup
    }
}
