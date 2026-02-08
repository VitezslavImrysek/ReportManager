using System.ComponentModel;

namespace ReportManager.Lib.Wpf
{
    public abstract class ViewModelBase : NotificationObject
    {
        protected static readonly bool IsInDesignMode =
            DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject());
    }
}
