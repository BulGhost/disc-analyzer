using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DiscAnalyzerModel.Enums;
using DiscAnalyzerModel.HelperClasses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerModel
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private static event Action BasePropertyForPercentOfParentCalculationChanged;
        private const long _percentOfRootItemAttributeToBeLarge = 15;
        private static ILogger _logger;
        private static ItemBaseProperty _basePropertyForPercentOfParentCalculation;
        private readonly object _threadLock = new();

        #region Properties

        public static ItemBaseProperty BasePropertyForPercentOfParentCalculation
        {
            get => _basePropertyForPercentOfParentCalculation;
            set
            {
                if (_basePropertyForPercentOfParentCalculation == value) return;

                _basePropertyForPercentOfParentCalculation = value;
                BasePropertyForPercentOfParentCalculationChanged?.Invoke();
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

        public static (Task task, FileSystemItem resultItem) CreateItemAsync(string fullPath,
            ItemBaseProperty basePropertyForPercentOfParentCalculation, ILogger logger, CancellationToken token)
        {
            _logger = logger;
            var item = new FileSystemItem { FullPath = fullPath };
            item.Root = item;
            item.IsLargeItem = true;
            item.PercentOfParent = 1000;

            BasePropertyForPercentOfParentCalculation = basePropertyForPercentOfParentCalculation;
            BasePropertyForPercentOfParentCalculationChanged += item.CountPercentOfParentForAllChildren;
            BasePropertyForPercentOfParentCalculationChanged += item.FindLargeItems;

            return (item.InitializeAsync(token), item);
        }

        private async Task<FileSystemItem> InitializeAsync(CancellationToken token)
        {
            _logger.LogInformation("Start {0} initialization", FullPath);
            token.ThrowIfCancellationRequested();
            await SetUpItemAttributesAsync(token).ConfigureAwait(false);
            await Task.Run(ChangeAttributesOfAllParentsInTree, token).ConfigureAwait(false);
            await GetChildrenOfItemAsync(token).ConfigureAwait(false);
            if (Children != null && Children.Count > 0)
                await Task.Run(CountPercentOfParentForAllChildren, token);
            if (Root == this && Size != 0) await Task.Run(FindLargeItems, token);
            return this;
        }

        private async Task SetUpItemAttributesAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                FileAttributes attr = await Task.Run(() => File.GetAttributes(FullPath), token)
                    .ConfigureAwait(false);
                if (attr.HasFlag(FileAttributes.Directory))
                {
                    var info = new DirectoryInfo(FullPath);
                    Type = info.Parent == null ? DirectoryItemType.Drive : DirectoryItemType.Folder;
                    SetUpDirectoryAttributes(info, token);
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

        private void SetUpDirectoryAttributes(DirectoryInfo info, CancellationToken token)
        {
            _logger.LogInformation("Start setting up file attributes on path {0}", FullPath);
            token.ThrowIfCancellationRequested();
            Name = Root == this ? info.FullName : info.Name;
            LastModified = info.LastWriteTime;
            Children = new ObservableCollection<FileSystemItem>();
        }

        private async Task SetUpFileAttributesAsync(CancellationToken token)
        {
            _logger.LogInformation("Start setting up directory attributes on path {0}", FullPath);
            token.ThrowIfCancellationRequested();
            var info = new FileInfo(FullPath);

            Name = info.Name;
            LastModified = info.LastWriteTime;
            Folders = 0;
            Files = 1;
            Size = info.Length;
            Allocated = await Task.Run(() => new FileSizeOnDiskDeterminator().GetFileSizeOnDisk(info), token);
        }

        private void ChangeAttributesOfAllParentsInTree()
        {
            _logger.LogInformation("Changing of attributes for all patents of file {1}", FullPath);
            FileSystemItem parentInTree = Parent;
            while (parentInTree != null)
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
        }

        private async Task GetChildrenOfItemAsync(CancellationToken token)
        {
            if (Type == DirectoryItemType.File) return;

            token.ThrowIfCancellationRequested();
            _logger.LogInformation("Start getting children of {0}", FullPath);
            List<string> childrenFullPaths = new DirectoryStructure(FullPath, _logger).GetDirectoryContents();
            FileSystemItem filesNode = GetSingleNodeForAllFiles();

            var tasks = new Task[childrenFullPaths.Count];
            for (int i = 0; i < tasks.Length; i++)
            {
                string path = childrenFullPaths[i];
                tasks[i] = AddNewChildItemAsync(Children, filesNode, path, token);
            }

            await Task.WhenAll(tasks);
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
                    if (filesNode.Files == 0) children.Add(filesNode);
                    AddFileItemToNode(newItem, filesNode);
                }
                else
                {
                    children.Add(newItem);
                }
            }
        }

        private static Task<FileSystemItem> CreateChildAsync(string fullPath, FileSystemItem rootItem,
            FileSystemItem parentItem, CancellationToken token)
        {
            var item = new FileSystemItem { FullPath = fullPath, Root = rootItem, Parent = parentItem };
            BasePropertyForPercentOfParentCalculationChanged += item.CountPercentOfParentForAllChildren;
            BasePropertyForPercentOfParentCalculationChanged += item.FindLargeItems;

            return item.InitializeAsync(token);
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

        private void CountPercentOfParentForAllChildren()
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

        private void CalculatePercentOfParent()
        {
            if (Parent == null)
            {
                PercentOfParent = 1000;
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
            if (total == 0) return 0;

            return (int)Math.Round(part * 1000D / total);
        }

        private void FindLargeItems()
        {
            _logger.LogInformation("Start searching for large items");
            foreach (FileSystemItem child in Children)
            {
                child.IsLargeItem = BasePropertyForPercentOfParentCalculation switch
                {
                    ItemBaseProperty.Size => child.Size * 100 / Root.Size >= _percentOfRootItemAttributeToBeLarge,
                    ItemBaseProperty.Allocated => child.Allocated * 100 / Root.Allocated >= _percentOfRootItemAttributeToBeLarge,
                    ItemBaseProperty.Files => child.Files * 100 / Root.Files >= _percentOfRootItemAttributeToBeLarge,
                    _ => false
                };

                if (child.Children != null) child.FindLargeItems();
            }
        }
    }
}