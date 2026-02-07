using ReportAdmin.App.Messages;
using ReportAdmin.Core.Models;
using ReportManager.Lib.Wpf;
using ReportManager.Shared;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ReportAdmin.App.ViewModels
{
    public class ReportHeaderViewModel : DataEditorVM<ReportSqlDocument, object>
    {
        #region Ctor

        public ReportHeaderViewModel()
        {
            RegisterMessage<GetCultureMessage>(OnGetCultureMessageReceived);
            RegisterMessage<GetReportKeyMessage>(OnGetReportKeyMessageReceived);
        }

        #endregion

        #region Properties

        [Required]
        [Description("Report key")]
        public string? Key { get; set => SetValue(ref field, value); }

        [Required]
        [Description("View schema")]
        public string? ViewSchema { get; set => SetValue(ref field, value); }

        [Required]
        [Description("View name")]
        public string? ViewName { get; set => SetValue(ref field, value); }

        [Required]
        [Description("Default culture")]
        public string? DefaultCulture { get; set => SetValue(ref field, value); }

        #endregion

        #region Commands

        public RelayCommand? ImportColumnsCommand { get; set => SetValue(ref field, value); }

        #endregion

        #region Protected Override Methods

        protected override void OnGetData(ReportSqlDocument data)
        {
            data.ReportKey = Key!;
            data.ViewSchema = ViewSchema!;
            data.ViewName = ViewName!;
            data.Definition.DefaultCulture = DefaultCulture!;
        }

        protected override void OnNew(object context)
        {

        }

        protected override void OnSetData(ReportSqlDocument data)
        {
            Key = data.ReportKey;
            ViewSchema = data.ViewSchema;
            ViewName = data.ViewName;
            DefaultCulture = data.Definition.DefaultCulture;
        }

        #endregion

        #region Private Methods

        private void OnGetCultureMessageReceived(GetCultureMessage message)
        {
            message.Culture = DefaultCulture ?? Constants.DefaultLanguage;
        }

        private void OnGetReportKeyMessageReceived(GetReportKeyMessage message)
        {
            message.ReportKey = Key;
        }

        #endregion
    }
}
