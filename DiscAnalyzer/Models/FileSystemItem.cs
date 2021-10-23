using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace DiscAnalyzer.Models
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private const long PercentToBeLargeItem = 15;

        private static readonly EnumerationOptions EnumOptions = new()
        {
            IgnoreInaccessible = true,
            RecurseSubdirectories = true
        };

        private long? _rootItemSize;

        public DirectoryItemType Type { get; set; }
        public string FullPath { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public long Allocated { get; set; }
        public int Files { get; set; }
        public int Folders { get; set; }
        public int PercentOfParent { get; set; }
        public DateTime LastModified { get; set; }
        public bool IsLargeItem { get; set; }
        public FileSystemItem Parent { get; set; }
        public ObservableCollection<FileSystemItem> Children { get; set; }

        private FileSystemItem()
        {
        }

        public FileSystemItem(string fullPath, bool isRoot = false,
            long? parentSize = null, long? rootItemSize = null)
        {
            SetUpItemAttributes(fullPath, isRoot, parentSize, rootItemSize);
            GetChildrenOfItemAsync();
        }

        private void SetUpItemAttributes(string fullPath, bool isRoot, long? parentSize, long? rootItemSize)
        {
            FileAttributes attr = File.GetAttributes(fullPath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var info = new DirectoryInfo(fullPath);
                Type = info.Parent == null ? DirectoryItemType.Drive : DirectoryItemType.Folder;
                SetUpDirectoryAttributes(info, isRoot, parentSize, rootItemSize);
                return;
            }

            Type = DirectoryItemType.File;
            SetUpFileAttributes(fullPath, parentSize, rootItemSize);
        }

        private void SetUpFileAttributes(string fullPath, long? parentSize, long? rootItemSize)
        {
            var info = new FileInfo(fullPath);

            FullPath = info.FullName;
            Name = info.Name;
            Size = info.Length;
            Allocated = GetFileSizeOnDisk(info);
            Files = 1;
            Folders = 0;
            LastModified = info.LastWriteTime;
            PercentOfParent = (int)(parentSize == null ? 1000 : Allocated * 1000 / parentSize);
            _rootItemSize = rootItemSize ?? Size;
            IsLargeItem = _rootItemSize != 0 && Size * 100 / _rootItemSize >= PercentToBeLargeItem;
        }

        private void SetUpDirectoryAttributes(DirectoryInfo info, bool isRoot, long? parentSize, long? rootItemSize)
        {
            FullPath = info.FullName;
            Name = isRoot ? info.FullName : info.Name;
            LastModified = info.LastWriteTime;
            Task.Run(() =>
            {
                var allDirectoryFiles = info.GetFiles("*", EnumOptions);
                var allSubdirectories = info.GetDirectories("*", EnumOptions);
                Files = allDirectoryFiles.Length;
                Folders = allSubdirectories.Length;
                Size = allDirectoryFiles.Sum(file => file.Length);
                _rootItemSize = rootItemSize ?? Size;
                IsLargeItem = _rootItemSize != 0 && Size * 100 / _rootItemSize >= PercentToBeLargeItem;
                Task.Run(() =>
                {
                    Allocated = allDirectoryFiles.Sum(GetFileSizeOnDisk);
                    PercentOfParent = (int) (parentSize == null ? 1000 : Allocated * 1000 / parentSize);
                });
            });
        }

        private static long GetFileSizeOnDisk(FileInfo info)
        {
            if (info.Directory == null) return 0;

            int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out uint sectorsPerCluster,
                out uint bytesPerSector, out _, out _);
            if (result == 0) throw new Win32Exception();

            uint clusterSize = sectorsPerCluster * bytesPerSector;
            uint losize = GetCompressedFileSizeW(info.FullName, out uint hosize);
            long size = ((long)hosize << 32) | losize;
            return (size + clusterSize - 1) / clusterSize * clusterSize;
        }

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        private async void GetChildrenOfItemAsync()
        {
            if (Type == DirectoryItemType.File /*|| item.Files == 0 && item.Folders == 0*/) return;

            Children = new ObservableCollection<FileSystemItem>();
            List<string> childrenFullPaths = GetDirectoryContents(FullPath);
            FileSystemItem filesNode = GetNodeForAllFiles();

            //foreach (string path in childrenFullPaths)
            //    await Task.Run(() => AddNewChildItem(Children, filesNode, path));
            await Task.Run(() => Parallel.ForEach(childrenFullPaths, path =>
                AddNewChildItem(Children, filesNode, path)));
        }

        public static List<string> GetDirectoryContents(string fullPath)
        {
            var items = new List<string>();
            var options = new EnumerationOptions {IgnoreInaccessible = true};
            try
            {
                string[] dirs = Directory.GetDirectories(fullPath, "*", options);
                if (dirs.Length > 0)
                    items.AddRange(dirs);

                string[] files = Directory.GetFiles(fullPath, "*", options);
                if (files.Length > 0)
                    items.AddRange(files);
            }
            catch (Exception ex)
            {
                //TODO: Add exception handler
                Console.WriteLine(ex);
                throw;
            }

            return items;
        }

        private FileSystemItem GetNodeForAllFiles() => new()
        {
            Type = DirectoryItemType.File,
            Children = new ObservableCollection<FileSystemItem>()
        };

        private void AddNewChildItem(ObservableCollection<FileSystemItem> children, //TODO: fix bag with parentSize parameter
            FileSystemItem filesNode, string pathToNewChild)
        {
            var newItem = new FileSystemItem(pathToNewChild, parentSize: Allocated, rootItemSize: _rootItemSize);
            lock (this)
            {
                if (newItem.Type == DirectoryItemType.File)
                {
                    if (filesNode.Files == 0) Application.Current.Dispatcher.Invoke(() => children.Add(newItem));
                    AddFileItemToNode(newItem, filesNode);
                    filesNode.PercentOfParent = (int)(filesNode.Size * 1000 / Size);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() => children.Add(newItem));
                    //children.Add(newItem);
                }
            }
        }

        private void AddFileItemToNode(FileSystemItem newItem, FileSystemItem node)
        {
            node.Files++;
            node.Name = $"[{node.Files} files]";
            node.Size += newItem.Size;
            node.Allocated += newItem.Allocated;

            if (node.LastModified < newItem.LastModified)
                node.LastModified = newItem.LastModified;

            node.Children.Add(newItem);
        }

        //private ObservableCollection<FileSystemItem> GetChildrenOfItem(FileSystemItemMod item)
        //{
        //    if (item.Type == DirectoryItemType.File || item.Files == 0 && item.Folders == 0)
        //        return null;

        //    var children = new ObservableCollection<FileSystemItem>();
        //    List<string> childrenFullPaths = DirectoryStructure.GetDirectoryContents(item.FullPath);
        //    FileSystemItem filesNode = GetNodeForAllFiles();

        //    var tasks = new Task[childrenFullPaths.Count];
        //    for (int i = 0; i < tasks.Length; i++)
        //    {
        //        string path = childrenFullPaths[i];
        //        tasks[i] = Task.Run(() => AddNewChildItem(children, filesNode, path));
        //    }

        //    Task.WaitAll(tasks);
        //    return children;
        //}
    }
}
