using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;

namespace DiscAnalyzer.ViewModels
{
    public class FileSystemItemViewModel : BaseViewModel
    {
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

        public FileSystemItemViewModel(string fullPath, bool isRoot = false, long? parentSize = null)
        {
            var item = DirectoryStructure.GetFileSystemItem(fullPath);

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
                    var newItem = new FileSystemItemViewModel(path, parentSize: Size);
                    lock (this)
                    {
                        children.Add(newItem);
                    }
                });
            }

            Task.WaitAll(tasks);
            return children;
        }
    }
}
