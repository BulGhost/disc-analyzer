using System.ComponentModel;
using System.Windows.Controls;

namespace DiscAnalyzer.HelperClasses
{
    public class ColumnHeaders : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public GridViewColumnHeader NameColumnHeader { get; set; }
        public GridViewColumnHeader SizeColumnHeader { get; set; }
        public GridViewColumnHeader AllocatedColumnHeader { get; set; }
        public GridViewColumnHeader FilesColumnHeader { get; set; }
        public GridViewColumnHeader FoldersColumnHeader { get; set; }
        public GridViewColumnHeader PercentOfParentColumnHeader { get; set; }
        public GridViewColumnHeader LastModifiedColumnHeader { get; set; }
    }
}
