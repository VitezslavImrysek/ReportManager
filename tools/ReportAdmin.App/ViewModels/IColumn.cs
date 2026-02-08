using ReportAdmin.App.Models.Definition;
using System.Collections.Generic;
using System.ComponentModel;

namespace ReportAdmin.App.ViewModels
{
    public interface IColumn : INotifyPropertyChanged
    {
        string Key { get; set; }
        IReadOnlyList<string> CategoryPath { get; }
        bool Hidden { get; set; }
        bool Filterable { get; set; }
        bool Sortable { get; set; }

        FilterConfigUi? Filter { get; set; }
        SortConfigUi? Sort { get; set; }
    }
}
