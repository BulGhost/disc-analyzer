using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DiscAnalyzer.Models
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private const long PercentToBeLargeItem = 15;
        private static readonly Dispatcher Dispatcher = Application.Current.Dispatcher;
        private static readonly object ThreadLock = new();

        #region Properties

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
        public FileSystemItem Root { get; set; }
        public FileSystemItem Parent { get; set; }
        public ObservableCollection<FileSystemItem> Children { get; set; }
        internal uint СlusterSize { get; set; }

        #endregion

        private FileSystemItem()
        {
        }

        public static (Task task, FileSystemItem resultItem) CreateItemAsync
            (string fullPath, CancellationToken token, FileSystemItem rootItem = null, FileSystemItem parentItem = null)
        {
            var item = new FileSystemItem { FullPath = fullPath, Root = rootItem, Parent = parentItem };
            if (rootItem == null)
            {
                item.Root = item;
                item.IsLargeItem = true;
                item.PercentOfParent = 1000;
            }

            return (item.InitializeAsync(token), item);
        }

        public static Task<FileSystemItem> CreateAsync(string fullPath, FileSystemItem rootItem = null,
            FileSystemItem parentItem = null, CancellationToken token = default)
        {
            var item = new FileSystemItem {FullPath = fullPath, Root = rootItem, Parent = parentItem};
            if (rootItem == null)
            {
                item.Root = item;
                item.IsLargeItem = true;
                item.PercentOfParent = 1000;
            }

            return item.InitializeAsync(token);
        }

        private async Task<FileSystemItem> InitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await SetUpItemAttributesAsync(token).ConfigureAwait(false);
            await GetChildrenOfItemAsync(token);
            if (Children != null && Children.Count > 0) await Task.Run(CountPercentOfParentForAllChildren, token);
            if (Root == this && Size != 0) await Task.Run(() => FindLargeItems(Children), token);
            return this;
        }

        private async Task SetUpItemAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            FileAttributes attr = File.GetAttributes(FullPath);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var info = new DirectoryInfo(FullPath);
                Type = info.Parent == null ? DirectoryItemType.Drive : DirectoryItemType.Folder;
                await SetUpDirectoryAttributesAsync(info, token);
                return;
            }

            Type = DirectoryItemType.File;
            await SetUpFileAttributesAsync(token);
        }

        private async Task SetUpDirectoryAttributesAsync(DirectoryInfo info, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Name = Root == this ? info.FullName : info.Name;
            LastModified = info.LastWriteTime;
            Children = new ObservableCollection<FileSystemItem>();
            СlusterSize = Root != this ? Root.СlusterSize : await Task.Run(() => GetClusterSize(info), token);
            if (Parent != null) await Task.Run(() => ChangeAttributesOfAllParentsInTree(nameof(Folders), this), token);
        }

        private async Task SetUpFileAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(FullPath);

            СlusterSize = Root.СlusterSize;
            Name = info.Name;
            LastModified = info.LastWriteTime;
            Folders = 0;
            Files = 1;
            await Task.Run(() => ChangeAttributesOfAllParentsInTree(nameof(Files), this), token);
            Size = info.Length;
            await Task.Run(() => ChangeAttributesOfAllParentsInTree(nameof(Size), this), token);
            Allocated = await Task.Run(() => GetFileSizeOnDisk(info), token);
            await Task.Run(() => ChangeAttributesOfAllParentsInTree(nameof(Allocated), this), token);
        }

        private static uint GetClusterSize(DirectoryInfo info)
        {
            int result = GetDiskFreeSpaceW(info.Root.FullName, out uint sectorsPerCluster,
                out uint bytesPerSector, out _, out _);
            if (result == 0) throw new Win32Exception();

            return sectorsPerCluster * bytesPerSector;
        }

        private long GetFileSizeOnDisk(FileInfo info)
        {
            uint losize = GetCompressedFileSizeW(info.FullName, out uint hosize);
            long size = ((long)hosize << 32) | losize;
            return (size + СlusterSize - 1) / СlusterSize * СlusterSize;
        }

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        private static void ChangeAttributesOfAllParentsInTree(string attributeName, FileSystemItem item)
        {
            FileSystemItem parentInTree = item.Parent;
            while (parentInTree != null)
            {
                lock (ThreadLock)
                {
                    switch (attributeName)
                    {
                        case nameof(Allocated):
                            parentInTree.Allocated += item.Allocated;
                            break;
                        case nameof(Size):
                            parentInTree.Size += item.Size;
                            break;
                        case nameof(Files):
                            parentInTree.Files++;
                            break;
                        case nameof(Folders):
                            parentInTree.Folders++;
                            break;
                    }
                }

                parentInTree = parentInTree.Parent;
            }
        }

        private void CountPercentOfParentForAllChildren()
        {
            Parallel.ForEach(Children, child =>
            {
                child.CalculatePercentOfParent();
                if (child.Type == DirectoryItemType.File)
                    foreach (FileSystemItem file in child.Children)
                        file.CalculatePercentOfParent();
            });
        }

        private void CalculatePercentOfParent()
        {
            if (Parent == null)
                PercentOfParent = 1000;
            else if (Parent.Allocated == 0)
                PercentOfParent = 0;
            else
                PercentOfParent = (int) Math.Round(Allocated * 1000D / Parent.Allocated);
        }

        private void FindLargeItems(ICollection<FileSystemItem> children)
        {
            foreach (FileSystemItem child in children)
            {
                child.IsLargeItem = child.Size * 100 / Size >= PercentToBeLargeItem;
                if (child.Children != null) FindLargeItems(child.Children);
            }
        }

        private async Task GetChildrenOfItemAsync(CancellationToken token)
        {
            if (Type == DirectoryItemType.File) return;

            token.ThrowIfCancellationRequested();
            List<string> childrenFullPaths = GetDirectoryContents(FullPath);
            FileSystemItem filesNode = GetSingleNodeForAllFiles();

            var tasks = new Task[childrenFullPaths.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                string path = childrenFullPaths[i];
                tasks[i] = AddNewChildItemAsync(Children, filesNode, path, token);
            }

            await Task.WhenAll(tasks);
        }

        private static List<string> GetDirectoryContents(string fullPath)
        {
            var items = new List<string>();
            var options = new EnumerationOptions {IgnoreInaccessible = true, AttributesToSkip = 0};
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

        private FileSystemItem GetSingleNodeForAllFiles() => new()
        {
            Type = DirectoryItemType.File,
            Parent = this,
            Root = this.Root,
            Children = new ObservableCollection<FileSystemItem>()
        };

        private async Task AddNewChildItemAsync(ObservableCollection<FileSystemItem> children,
            FileSystemItem filesNode, string pathToNewChild, CancellationToken token)
        {
            FileSystemItem newItem = await CreateAsync(pathToNewChild, Root, this, token);
            lock (this)
            {
                token.ThrowIfCancellationRequested();
                if (newItem.Type == DirectoryItemType.File)
                {
                    if (filesNode.Files == 0) Dispatcher.Invoke(() => children.Add(filesNode), DispatcherPriority.Loaded, token);
                    AddFileItemToNode(newItem, filesNode, token);
                }
                else
                {
                    Dispatcher.Invoke(() => children.Add(newItem), DispatcherPriority.Loaded, token);
                }
            }
        }

        private static void AddFileItemToNode(FileSystemItem newItem, FileSystemItem node, CancellationToken token)
        {
            node.Files++;
            node.Name = $"[{node.Files} files]";
            node.Size += newItem.Size;
            node.Allocated += newItem.Allocated;

            if (node.LastModified < newItem.LastModified)
                node.LastModified = newItem.LastModified;

            Dispatcher.Invoke(() => node.Children.Add(newItem), DispatcherPriority.Loaded, token);
        }
    }
}
