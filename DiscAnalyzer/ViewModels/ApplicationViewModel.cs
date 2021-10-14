using System.Collections;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Aga.Controls.Tree;
using DiscAnalyzer.Commands;
using DiscAnalyzer.Models;

namespace DiscAnalyzer.ViewModels
{
    public class ApplicationViewModel : BaseViewModel, ITreeModel
    {
        private readonly FileSystemItemViewModel _rootItem;
        private ICommand _openDialogCommand = null;


        public ApplicationViewModel(string directoryFullPath = null)
        {
            _rootItem = directoryFullPath != null ? new FileSystemItemViewModel(directoryFullPath, true) : null;
        }

        public ICommand OpenDialogCmd =>
            _openDialogCommand ??= new SelectDirectoryCommand();

        public IEnumerable GetChildren(object parent)
        {
            return parent == null
                ? new ObservableCollection<FileSystemItemViewModel> { _rootItem }
                : (parent as FileSystemItemViewModel)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItemViewModel item && item.Type != DirectoryItemType.File &&
                   (item.Files != 0 || item.Folders != 0);
        }
    }
}
