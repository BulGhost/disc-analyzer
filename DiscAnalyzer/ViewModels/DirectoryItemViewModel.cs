using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;
using DiscAnalyzer.ViewModels.Base;

namespace DiscAnalyzer.ViewModels
{
    public class DirectoryItemViewModel : BaseViewModel
    {
        public DirectoryItemType Type { get; set; }

        public string FullPath { get; set; }

        public string Name => Type == DirectoryItemType.Drive
            ? FullPath
            : DirectoryStructure.GetFileFolderName(FullPath);

        public ObservableCollection<DirectoryItemViewModel> Children { get; set; }

        public bool CanExpand => Type != DirectoryItemType.File;

        public bool IsExpanded
        {
            get
            {
                return Children?.Count(f => f != null) > 0;
            }
            set
            {
                if (value)
                    Expand();
                else
                    ClearChildren();
            }
        }

        public ICommand ExpandCommand { get; set; }

        public DirectoryItemViewModel(string fullPath, DirectoryItemType type)
        {
            ExpandCommand = new RelayCommand(Expand);
            FullPath = fullPath;
            Type = type;

            ClearChildren();
        }

        private void Expand()
        {
            if (Type == DirectoryItemType.File) return;

            var children = DirectoryStructure.GetDirectoryContents(FullPath);
            Children = new ObservableCollection<DirectoryItemViewModel>(children
                .Select(content => new DirectoryItemViewModel(content.FullPath, content.Type)));
        }

        private void ClearChildren()
        {
            Children = new ObservableCollection<DirectoryItemViewModel>();

            if (Type != DirectoryItemType.File) Children.Add(null);
        }
    }
}
