using System.Collections.ObjectModel;
using System.Linq;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;
using DiscAnalyzer.ViewModels.Base;

namespace DiscAnalyzer.ViewModels
{
    public class DirectoryStructureViewModel : BaseViewModel
    {
        public ObservableCollection<DirectoryItemViewModel> Items { get; set; }

        public DirectoryStructureViewModel()
        {
            var children = DirectoryStructure.GetLogicalDrives();
            Items = new ObservableCollection<DirectoryItemViewModel>(children
                .Select(drive => new DirectoryItemViewModel(drive.FullPath, DirectoryItemType.Drive)));
        }
    }
}
