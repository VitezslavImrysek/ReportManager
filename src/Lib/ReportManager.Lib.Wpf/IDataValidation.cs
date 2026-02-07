using System.Text;

namespace ReportManager.Lib.Wpf
{ 
    public interface IDataValidation
    {
        bool Validate(StringBuilder log);
    }
}
