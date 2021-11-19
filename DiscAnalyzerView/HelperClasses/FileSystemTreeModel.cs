using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Aga.Controls.Tree;
using DiscAnalyzerModel;
using DiscAnalyzerViewModel;

namespace DiscAnalyzerView.HelperClasses
{
    public class FileSystemTreeModel : ITreeModel
    {
        private readonly ApplicationViewModel _appViewModel;
        private FileSystemItem _rootItem;
        private readonly TreeList _treeList;

        public FileSystemTreeModel(TreeList treeList, ApplicationViewModel appViewModel)
        {
            _treeList = treeList ?? throw new ArgumentNullException(nameof(treeList));
            _appViewModel = appViewModel ?? throw new ArgumentNullException(nameof(appViewModel));
            appViewModel.PropertyChanged += AppViewModelOnPropertyChanged;
        }

        public IEnumerable GetChildren(object parent)
        {
            return parent == null
                ? new ObservableCollection<FileSystemItem> { _rootItem }
                : (parent as FileSystemItem)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItem item && item.Children != null;
        }

        private void AppViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ApplicationViewModel.RootItem))
            {
                _rootItem = _appViewModel.RootItem;
                _treeList.UpdateNodes();
                if (_treeList.Nodes.Count != 0)
                {
                    _treeList.Nodes[0].IsExpanded = true;
                }
            }
        }
    }
}
