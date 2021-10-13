using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aga.Controls.Tree;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;
using DiscAnalyzer.ViewModels.Base;

namespace DiscAnalyzer.ViewModels
{
    public class FileSystemItemViewModel : BaseViewModel, ITreeModel
    {
        public bool IsRootDirectory { get; set; } = false;

        public DirectoryItemType Type { get; set; }

        public string FullPath { get; set; }

        //public string Name => Type == DirectoryItemType.Drive
        //    ? FullPath
        //    : DirectoryStructure.GetFileFolderName(FullPath);

        public string Name { get; set; }

        public long Size { get; set; }

        public long Allocated { get; set; }

        public int Files { get; set; }

        public int Folders { get; set; }

        public int PercentOfParent { get; set; }

        public DateTime LastModified { get; set; }

        public ObservableCollection<FileSystemItemViewModel> Children { get; set; }

        public bool CanExpand => Type != DirectoryItemType.File && (Files != 0 || Folders != 0);

        public bool IsExpanded { get; set; }

        public ICommand ExpandCommand { get; set; }

        public FileSystemItemViewModel(string fullPath, bool isRoot = false)
        {
            var item = DirectoryStructure.GetFileSystemItem(fullPath);

            IsRootDirectory = isRoot;
            Type = item.Type;
            FullPath = item.FullPath;
            Name = isRoot ? item.FullPath : item.Name;
            Size = item.Size;
            Allocated = item.Allocated;
            Files = item.Files;
            Folders = item.Folders;
            PercentOfParent = isRoot ? 1000 : 0; //UNDONE: PercentOfParentProperty
            LastModified = item.LastModified;
            Children = GetChildrenOfItem(item);
            IsExpanded = isRoot;

            //ExpandCommand = new RelayCommand(Expand);
        }

        private ObservableCollection<FileSystemItemViewModel> GetChildrenOfItem(FileSystemItem item)
        {
            if (item.Type == DirectoryItemType.File || item.Files == 0 && item.Folders == 0)
                return null;

            var children = new ObservableCollection<FileSystemItemViewModel>();
            var childrenFullPaths = DirectoryStructure.GetDirectoryContents(item.FullPath);
            var tasks = new Task[childrenFullPaths.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                string path = childrenFullPaths[i];
                tasks[i] = Task.Run(() =>
                {
                    var newItem = new FileSystemItemViewModel(path);
                    lock (this)
                    {
                        children.Add(newItem);
                    }
                });
            }

            Task.WaitAll(tasks);
            return children;
        }

        //private void Expand()
        //{
        //    if (Type == DirectoryItemType.File) return;

        //    var children = DirectoryStructure.GetDirectoryContents(FullPath);
        //    Children = new ObservableCollection<FileSystemItemViewModel>(children
        //        .Select(content => new FileSystemItemViewModel(content.FullPath, content.Type)));
        //}

        //private void ClearChildren()
        //{
        //    Children = new ObservableCollection<FileSystemItemViewModel>();

        //    if (Type != DirectoryItemType.File) Children.Add(null);
        //}
        public IEnumerable GetChildren(object parent)
        {
            return parent == null
                ? new ObservableCollection<FileSystemItemViewModel> {new("D:\\Дмитрий\\Авто", true)}
                : (parent as FileSystemItemViewModel)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItemViewModel item && item.Type != DirectoryItemType.File;
        }
    }
}
