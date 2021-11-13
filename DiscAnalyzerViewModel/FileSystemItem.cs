using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using DiscAnalyzerViewModel.Enums;
using DiscAnalyzerViewModel.HelperClasses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerViewModel
{
    public enum DirectoryItemType
    {
        Drive,
        File,
        Folder
    }

    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private const long _percentToBeLargeItem = 15;
        private static readonly Dispatcher _dispatcher = Application.Current.Dispatcher;
        private static ILogger _logger;
        private readonly object _threadLock = new();
        private uint _clusterSize;

        #region Properties

        public static ItemProperty BasePropertyForPercentOfParentCalculation { get; set; }
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

        #endregion

        private FileSystemItem()
        {
        }

        public static (Task task, FileSystemItem resultItem) CreateItemAsync(string fullPath,
            ItemProperty basePropertyForPercentOfParentCalculation, ILogger logger, CancellationToken token)
        {
            _logger = logger;
            var item = new FileSystemItem { FullPath = fullPath };
            item.Root = item;
            item.IsLargeItem = true;
            item.PercentOfParent = 1000;

            BasePropertyForPercentOfParentCalculation = basePropertyForPercentOfParentCalculation;

            return (item.InitializeAsync(token), item);
        }

        public void CountPercentOfParentForAllChildren()
        {
            _logger.LogInformation("Start calculating percentage of parent for {0} children", FullPath);
            try
            {
                Parallel.ForEach(Children, child =>
                {
                    child.CalculatePercentOfParent();
                    if (child.Type != DirectoryItemType.File) return;

                    foreach (FileSystemItem file in child.Children)
                        file.CalculatePercentOfParent();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during percent of parent calculation");
                throw;
            }
        }

        private static Task<FileSystemItem> CreateChildAsync(string fullPath, FileSystemItem rootItem,
            FileSystemItem parentItem, CancellationToken token = default)
        {
            var item = new FileSystemItem { FullPath = fullPath, Root = rootItem, Parent = parentItem };

            return item.InitializeAsync(token);
        }

        private async Task<FileSystemItem> InitializeAsync(CancellationToken token)
        {
            _logger.LogInformation("Start {0} initialization", FullPath);
            token.ThrowIfCancellationRequested();
            await SetUpItemAttributesAsync(token).ConfigureAwait(false);
            await GetChildrenOfItemAsync(token);
            if (Children != null && Children.Count > 0)
                await Task.Run(CountPercentOfParentForAllChildren, token);
            if (Root == this && Size != 0) await Task.Run(() => FindLargeItems(Children), token);
            return this;
        }

        private async Task SetUpItemAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
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
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during setting up item attributes on path {0}", FullPath);
                throw;
            }
        }

        private async Task SetUpDirectoryAttributesAsync(DirectoryInfo info, CancellationToken token)
        {
            _logger.LogInformation("Start setting up file attributes on path {0}", FullPath);
            token.ThrowIfCancellationRequested();
            Name = Root == this ? info.FullName : info.Name;
            LastModified = info.LastWriteTime;
            Children = new ObservableCollection<FileSystemItem>();
            _clusterSize = Root != this
                ? Root._clusterSize
                : await Task.Run(() => new FileSizeOnDiskDeterminationHelper().GetClusterSize(info.FullName), token);
            if (Parent != null)
            {
                await Task.Run(() => ChangeAttributesOfAllParentsInTree(nameof(Folders), this), token);
            }
        }

        private async Task SetUpFileAttributesAsync(CancellationToken token)
        {
            _logger.LogInformation("Start setting up directory attributes on path {0}", FullPath);
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(FullPath);

            _clusterSize = Root._clusterSize;
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

        private long GetFileSizeOnDisk(FileInfo info)
        {
            uint losize = FileSizeOnDiskDeterminationHelper.GetCompressedFileSizeW(info.FullName, out uint hosize);
            long size = ((long)hosize << 32) | losize;
            return (size + _clusterSize - 1) / _clusterSize * _clusterSize;
        }

        private void ChangeAttributesOfAllParentsInTree(string attributeName, FileSystemItem item)
        {
            _logger.LogInformation("Changing of {0} attribute for all patents of file {1}", attributeName, item.FullPath);
            FileSystemItem parentInTree = item.Parent;
            while (parentInTree != null)
            {
                lock (parentInTree)
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

        private void CalculatePercentOfParent()
        {
            if (Parent == null)
            {
                PercentOfParent = 1000;
                return;
            }

            PercentOfParent = BasePropertyForPercentOfParentCalculation switch
            {
                ItemProperty.Size => FindPercent(Size, Parent.Size),
                ItemProperty.Allocated => FindPercent(Allocated, Parent.Allocated),
                ItemProperty.Files => FindPercent(Files, Parent.Files),
                _ => PercentOfParent
            };
        }

        private int FindPercent(long part, long total)
        {
            if (total == 0) return 0;

            return (int)Math.Round(part * 1000D / total);
        }

        private void FindLargeItems(ICollection<FileSystemItem> children)
        {
            _logger.LogInformation("Start searching for large items");
            foreach (FileSystemItem child in children)
            {
                child.IsLargeItem = child.Size * 100 / Size >= _percentToBeLargeItem;
                if (child.Children != null) FindLargeItems(child.Children);
            }
        }

        private async Task GetChildrenOfItemAsync(CancellationToken token)
        {
            if (Type == DirectoryItemType.File) return;

            token.ThrowIfCancellationRequested();
            _logger.LogInformation("Start getting children of {0}", FullPath);
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

        private List<string> GetDirectoryContents(string fullPath)
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
                _logger.LogError(ex, "Error during try to get directories and files inside {0}", fullPath);
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
            _logger.LogInformation("Try to add new child item on path {0}", pathToNewChild);
            FileSystemItem newItem = await CreateChildAsync(pathToNewChild, Root, this, token);
            lock (_threadLock)
            {
                token.ThrowIfCancellationRequested();
                if (newItem.Type == DirectoryItemType.File)
                {
                    if (filesNode.Files == 0) _dispatcher.Invoke(() => children.Add(filesNode), DispatcherPriority.Loaded, token);
                    AddFileItemToNode(newItem, filesNode, token);
                }
                else
                {
                    _dispatcher.Invoke(() => children.Add(newItem), DispatcherPriority.Loaded, token);
                }
            }
        }

        private void AddFileItemToNode(FileSystemItem newItem, FileSystemItem node, CancellationToken token)
        {
            node.Files++;
            node.Name = $"[{node.Files} files]";
            node.Size += newItem.Size;
            node.Allocated += newItem.Allocated;

            if (node.LastModified < newItem.LastModified)
                node.LastModified = newItem.LastModified;

            _dispatcher.Invoke(() => node.Children.Add(newItem), DispatcherPriority.Loaded, token);
        }
    }
}