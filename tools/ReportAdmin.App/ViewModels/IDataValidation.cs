using System.Text;

namespace ReportAdmin.App.ViewModels
{
    public interface IDataValidation
    {
        bool Validate(StringBuilder log);
    }
}
