using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;

namespace DiscAnalyzer.ViewModels
{
    public class FileSystemItemViewModel : BaseViewModel
    {
        private const long PercentToBeLargeItem = 15;
        private readonly long? _rootItemSize;

        public DirectoryItemType Type { get; set; }

        public string FullPath { get; set; }

        public string Name { get; set; }

        public long Size { get; set; }

        public long Allocated { get; set; }

        public int Files { get; set; }

        public int Folders { get; set; }

        public int PercentOfParent { get; set; }

        public DateTime LastModified { get; set; }

        public ObservableCollection<FileSystemItemViewModel> Children { get; set; }

        public bool IsLargeItem { get; set; }

        private FileSystemItemViewModel()
        {
        }

        public FileSystemItemViewModel(string fullPath, bool isRoot = false,
            long? parentSize = null, long? rootItemSize = null)
        {
            FileSystemItem item = DirectoryStructure.GetFileSystemItem(fullPath);
            _rootItemSize = rootItemSize ?? item.Size;

            Type = item.Type;
            FullPath = item.FullPath;
            Name = isRoot ? item.FullPath : item.Name;
            Size = item.Size;
            Allocated = item.Allocated;
            Files = item.Files;
            Folders = item.Folders;
            PercentOfParent = (int)(parentSize == null ? 1000 : (double)Size / parentSize * 1000);
            LastModified = item.LastModified;
            Children = GetChildrenOfItem(item);
            IsLargeItem = Size * 100 / _rootItemSize >= PercentToBeLargeItem;
        }

        private ObservableCollection<FileSystemItemViewModel> GetChildrenOfItem(FileSystemItem item)
        {
            if (item.Type == DirectoryItemType.File || item.Files == 0 && item.Folders == 0)
                return null;

            var children = new ObservableCollection<FileSystemItemViewModel>();
            List<string> childrenFullPaths = DirectoryStructure.GetDirectoryContents(item.FullPath);
            FileSystemItemViewModel filesNode = GetNodeForAllFiles();

            var tasks = new Task[childrenFullPaths.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                string path = childrenFullPaths[i];
                tasks[i] = Task.Run(() => AddNewChildItem(children, filesNode, path));
            }

            Task.WaitAll(tasks);
            return children;
        }

        private FileSystemItemViewModel GetNodeForAllFiles() => new()
        {
            Type = DirectoryItemType.File,
            Children = new ObservableCollection<FileSystemItemViewModel>()
        };

        private void AddNewChildItem(ObservableCollection<FileSystemItemViewModel> children,
            FileSystemItemViewModel filesNode, string pathToNewChild)
        {
            var newItem = new FileSystemItemViewModel(pathToNewChild, parentSize: Size, rootItemSize: _rootItemSize);
            lock (this)
            {
                if (newItem.Type == DirectoryItemType.File)
                {
                    if (filesNode.Files == 0) children.Add(filesNode);
                    AddFileItemToNode(newItem, filesNode);
                    filesNode.PercentOfParent = (int)((double)filesNode.Size / Size * 1000);
                }
                else
                {
                    children.Add(newItem);
                }
            }
        }


        private void AddFileItemToNode(FileSystemItemViewModel newItem, FileSystemItemViewModel node)
        {
            node.Files++;
            node.Name = $"[{node.Files} files]";
            node.Size += newItem.Size;
            node.Allocated += newItem.Allocated;

            if (node.LastModified < newItem.LastModified)
                node.LastModified = newItem.LastModified;

            node.Children.Add(newItem);
        }
    }
}
