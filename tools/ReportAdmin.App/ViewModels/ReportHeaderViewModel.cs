using ReportAdmin.Core.Models;

namespace ReportAdmin.App.ViewModels
{
    public class ReportHeaderViewModel : DataEditorVM<ReportSqlDocumentUi, object>
    {
        public string? Key { get; set => SetValue(ref field, value); }
        public string? ViewSchema { get; set => SetValue(ref field, value); }
        public string? ViewName { get; set => SetValue(ref field, value); }
        public string? DefaultCulture { get; set => SetValue(ref field, value); }

        public RelayCommand ImportColumnsCommand { get; set => SetValue(ref field, value); }

        protected override void OnGetData(ReportSqlDocumentUi data)
        {
            data.ReportKey = Key ?? string.Empty;
            data.ViewSchema = ViewSchema ?? string.Empty;
            data.ViewName = ViewName ?? string.Empty;
            data.Definition.DefaultCulture = DefaultCulture ?? string.Empty;
        }

        protected override void OnNew(object context)
        {

        }

        protected override void OnSetData(ReportSqlDocumentUi data)
        {
            Key = data.ReportKey;
            ViewSchema = data.ViewSchema;
            ViewName = data.ViewName;
            DefaultCulture = data.Definition.DefaultCulture;
        }
    }
}
