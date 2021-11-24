using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscAnalyzerModel.Enums;
using DiscAnalyzerModel.HelperClasses;
using DiscAnalyzerModel.Resourses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerModel
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static event Func<Task> BasePropertyChanged;
        private const int _maxPercent = 1000;
        private const long _percentOfRootItemAttributeToBeLarge = 150;
        private static ILogger _logger;
        private static ItemBaseProperty _baseProperty;
        private readonly object _threadLock = new();

        #region Properties

        public static ItemBaseProperty BasePropertyForPercentOfParentCalculation
        {
            get => _baseProperty;
            set
            {
                if (_baseProperty == value)
                {
                    return;
                }

                _baseProperty = value;
                BasePropertyChanged?.Invoke();
            }
        }

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

        internal static (Task task, FileSystemItem resultItem) CreateItemAsync(string fullPath,
            ItemBaseProperty basePropertyForPercentOfParentCalculation, ILogger logger, CancellationToken token)
        {
            _logger = logger;
            BasePropertyChanged = null;
            var item = new FileSystemItem { FullPath = fullPath };
            item.Root = item;
            item.IsLargeItem = true;
            item.Children = new ObservableCollection<FileSystemItem>();
            item.PercentOfParent = _maxPercent;

            BasePropertyForPercentOfParentCalculation = basePropertyForPercentOfParentCalculation;
            BasePropertyChanged += item.CalculatePercentOfParentForAllChildren;
            BasePropertyChanged += item.FindLargeItems;

            Task.Run(() => item.SetUpItemAttributesAsync(token), token).ConfigureAwait(false);

            return (item.InitializeAsync(token), item);
        }

        private async Task<FileSystemItem> InitializeAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _logger.LogDebug("Start {0} initialization", FullPath);
            await ChangeAttributesOfAllParentsInTree(token).ConfigureAwait(false);
            await GetChildrenOfItemAsync(token).ConfigureAwait(false);

            if (Children != null && Children.Count > 0)
            {
                await Task.Run(() => CalculatePercentOfParentForAllChildren(token), token);
                BasePropertyChanged += CalculatePercentOfParentForAllChildren;
            }

            if (Root == this)
            {
                await Task.Run(FindLargeItems, token);
            }

            _logger.LogDebug("{0} initialization completed", FullPath);
            return this;
        }

        private async Task SetUpItemAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _logger.LogDebug("Start setting up item attributes on path {0}", FullPath);
            try
            {
                FileAttributes attr = await Task.Run(() => File.GetAttributes(FullPath), token)
                    .ConfigureAwait(false);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var info = new DirectoryInfo(FullPath);
                    Type = info.Parent == null ? DirectoryItemType.Drive : DirectoryItemType.Folder;
                    token.ThrowIfCancellationRequested();
                    SetUpDirectoryAttributes(info);
                    return;
                }

                Type = DirectoryItemType.File;
                await SetUpFileAttributesAsync(token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error during setting up attributes on path {0}", FullPath);
            }

            _logger.LogDebug("Setting up item attributes on path {0} completed", FullPath);
        }

        private void SetUpDirectoryAttributes(DirectoryInfo info)
        {
            Name = Root == this ? info.FullName : info.Name;
            LastModified = info.LastWriteTime;
            Children ??= new ObservableCollection<FileSystemItem>();
        }

        private async Task SetUpFileAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(FullPath);

            Name = info.Name;
            LastModified = info.LastWriteTime;
            Folders = 0;
            Files = 1;
            Size = info.Length;
            Allocated = await Task.Run(() => new FileSizeOnDiskDeterminator().GetFileSizeOnDisk(info), token);
        }

        private Task ChangeAttributesOfAllParentsInTree(CancellationToken token)
        {
            _logger.LogDebug("Changing of attributes for all parents of item {0} started", FullPath);
            FileSystemItem parentInTree = Parent;
            return Task.Run(() =>
            {
                while (!token.IsCancellationRequested && parentInTree != null)
                {
                    lock (parentInTree)
                    {
                        if (Type == DirectoryItemType.File)
                        {
                            parentInTree.Allocated += Allocated;
                            parentInTree.Size += Size;
                            parentInTree.Files++;
                        }
                        else
                        {
                            parentInTree.Folders++;
                        }
                    }

                    parentInTree = parentInTree.Parent;
                }

                _logger.LogDebug("Changing of attributes for all parents of item {0} completed", FullPath);
            }, token);
        }

        private async Task GetChildrenOfItemAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _logger.LogDebug("Start getting children of {0}", FullPath);
            if (Type == DirectoryItemType.File)
            {
                _logger.LogDebug("Item {0} couldn't has children", FullPath);
                return;
            }

            List<string> childrenFullPaths = new DirectoryStructure(FullPath, _logger).GetDirectoryContents();
            FileSystemItem filesNode = GetSingleNodeForAllFiles();

            var tasks = new Task[childrenFullPaths.Count];
            for (int i = 0; i < tasks.Length && !token.IsCancellationRequested; i++)
            {
                string path = childrenFullPaths[i];
                tasks[i] = AddNewChildItemAsync(Children, filesNode, path, token);
            }

            token.ThrowIfCancellationRequested();
            await Task.WhenAll(tasks);
            _logger.LogDebug("Getting children of {0} completed", FullPath);
        }

        private FileSystemItem GetSingleNodeForAllFiles() => new()
        {
            Type = DirectoryItemType.File,
            FullPath = this.FullPath,
            Parent = this,
            Root = this.Root,
            Children = new ObservableCollection<FileSystemItem>()
        };

        private async Task AddNewChildItemAsync(ObservableCollection<FileSystemItem> children,
            FileSystemItem filesNode, string pathToNewChild, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            FileSystemItem newItem = await CreateChildAsync(pathToNewChild, Root, this, token);
            lock (_threadLock)
            {
                token.ThrowIfCancellationRequested();
                if (newItem.Type == DirectoryItemType.File)
                {
                    if (filesNode.Files == 0) children.Add(filesNode);
                    AddFileItemToNode(newItem, filesNode);
                }
                else
                {
                    children.Add(newItem);
                }
            }

            await newItem.InitializeAsync(token);
        }

        private async Task<FileSystemItem> CreateChildAsync(string fullPath, FileSystemItem rootItem,
            FileSystemItem parentItem, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var item = new FileSystemItem { FullPath = fullPath, Root = rootItem, Parent = parentItem };
            await item.SetUpItemAttributesAsync(token);
            return item;
        }

        private void AddFileItemToNode(FileSystemItem newItem, FileSystemItem node)
        {
            node.Files++;
            node.Name = string.Format(Resources.FilesNodeName, node.Files);
            node.Size += newItem.Size;
            node.Allocated += newItem.Allocated;

            if (node.LastModified < newItem.LastModified)
            {
                node.LastModified = newItem.LastModified;
            }

            node.Children.Add(newItem);
        }

        private void CalculatePercentOfParentForAllChildren(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _logger.LogDebug("Start calculating percent of parent for all children of {0}", FullPath);
            Parallel.ForEach(Children,
                new ParallelOptions {CancellationToken = token, MaxDegreeOfParallelism = 10},
                CalculatePercentOfParentForChild);
            _logger.LogDebug("Calculating percent of parent for all children of {0} completed", FullPath);
        }

        private Task CalculatePercentOfParentForAllChildren()
        {
            return Task.Run(() => CalculatePercentOfParentForAllChildren(default));
        }

        private void CalculatePercentOfParentForChild(FileSystemItem child)
        {
            child.CalculatePercentOfParent();
            if (child.Type == DirectoryItemType.File && child.Children != null)
            {
                child.CalculatePercentOfParentForAllChildren();
            }
        }

        private void CalculatePercentOfParent()
        {
            if (Parent == null)
            {
                PercentOfParent = _maxPercent;
                return;
            }

            PercentOfParent = BasePropertyForPercentOfParentCalculation switch
            {
                ItemBaseProperty.Size => FindPercent(Size, Parent.Size),
                ItemBaseProperty.Allocated => FindPercent(Allocated, Parent.Allocated),
                ItemBaseProperty.Files => FindPercent(Files, Parent.Files),
                _ => PercentOfParent
            };
        }

        private int FindPercent(long part, long total)
        {
            if (total == 0)
            {
                return 0;
            }

            return (int)Math.Round(part * (double)_maxPercent / total);
        }

        private async Task FindLargeItems()
        {
            _logger.LogDebug("Start searching of large items for path {0}", FullPath);
            if (Root.Size == 0 || Root.Files == 0)
            {
                return;
            }

            foreach (FileSystemItem child in Children)
            {
                child.IsLargeItem = BasePropertyForPercentOfParentCalculation switch
                {
                    ItemBaseProperty.Size => child.Size * _maxPercent / Root.Size >= _percentOfRootItemAttributeToBeLarge,
                    ItemBaseProperty.Allocated => child.Allocated * _maxPercent / Root.Allocated >= _percentOfRootItemAttributeToBeLarge,
                    ItemBaseProperty.Files => child.Files * _maxPercent / Root.Files >= _percentOfRootItemAttributeToBeLarge,
                    _ => false
                };

                if (child.Children != null && child.Children.Count != 0 && child.IsLargeItem)
                {
                    await child.FindLargeItems();
                }
            }

            _logger.LogDebug("Searching of large items for path {0} completed", FullPath);
        }
    }
}