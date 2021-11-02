using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Aga.Controls.Tree;
using AsyncAwaitBestPractices.MVVM;
using DiscAnalyzer.Commands;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.HelperClasses.Converters;
using Microsoft.WindowsAPICodePack.Dialogs;
using MenuItem = DiscAnalyzer.HelperClasses.MenuItem;

namespace DiscAnalyzer
{
    public enum ItemProperty
    {
        Size,
        Allocated,
        Files,
        PercentOfParent
    }

    public enum Unit
    {
        Auto,
        Kb,
        Mb,
        Gb
    }

    public enum ExpandLevel
    {
        Level1,
        Level2,
        Level3,
        Level4,
        Level5,
        FullExpand
    }

    public class ApplicationViewModel : INotifyPropertyChanged, ITreeModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Constants

        public const string DriveCategoryName = "Drives";
        public const string DirectoryCategoryName = "Directory";

        private const string NameColumnHeaderName = "Name";
        private const string SizeColumnHeaderName = "Size";
        private const string AllocatedColumnHeaderName = "Allocated";
        private const string FilesColumnHeaderName = "Files";
        private const string FoldersColumnHeaderName = "Folders";
        private const string PercentOfParentColumnHeaderName = "% of Parent";
        private const string LastModifiedColumnHeaderName = "Last Modified";

        #endregion

        #region Fields

        private FileSystemItem _rootItem;
        private GridViewColumnHeader _treeListSortColumn;
        private SortAdorner _treeListSortAdorner;
        private ListCollectionView _view;
        private IAsyncCommand _openDialogCommand;
        private RelayCommand _stopCommand;
        private IAsyncCommand _refreshCommand;
        private bool _hasRootItem;
        private RelayCommand<GridViewColumnHeader> _sortCommand;
        private IAsyncCommand<ExpandLevel> _expandCommand;
        private Task _directoryAnalysis;
        private ItemProperty _mode;

        #endregion

        #region Properties

        public TreeList TreeList { get; }
        public ListCollectionView SelectDirectoryMenuItems { get; }

        public ItemProperty Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                if (_mode == ItemProperty.PercentOfParent) return;

                FileSystemItem.BasePropertyForPercentOfParentCalculation = _mode;
                PercentOfParentColumnHeader.Content = GetPercentOfParentColumnHeaderName();
                _rootItem?.CountPercentOfParentForAllChildren();
            }
        }

        public Unit Unit { get; set; }

        public GridViewColumnHeader NameColumnHeader { get; set; }
        public GridViewColumnHeader SizeColumnHeader { get; set; }
        public GridViewColumnHeader AllocatedColumnHeader { get; set; }
        public GridViewColumnHeader FilesColumnHeader { get; set; }
        public GridViewColumnHeader FoldersColumnHeader { get; set; }
        public GridViewColumnHeader PercentOfParentColumnHeader { get; set; }
        public GridViewColumnHeader LastModifiedColumnHeader { get; set; }
        public CancellationTokenSource Source { get; set; }
        public bool CanStop { get; set; }

        public bool HasRootItem
        {
            get => _hasRootItem;
            set
            {
                if (_hasRootItem != value)
                {
                    _hasRootItem = value;
                    RefreshCommand.RaiseCanExecuteChanged();
                    ExpandCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string DiscFreeSpaceInfo { get; set; }
        public string FilesCountInfo { get; set; }
        public string ClusterSizeInfo { get; set; }

        #endregion

        public ApplicationViewModel(TreeList treeList)
        {
            TreeList = treeList;
            SelectDirectoryMenuItems = GetSelectDirectoryMenuItems();
            SetUpColumnsHeaders();
        }

        #region ITreeModel implementation

        public IEnumerable GetChildren(object parent)
        {
            return parent == null ? new ObservableCollection<FileSystemItem> {_rootItem} : (parent as FileSystemItem)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItem item && item.Children != null;
        }

        #endregion

        #region Commands

        public IAsyncCommand OpenDialogCommand =>
            _openDialogCommand ??= new AsyncCommand(async () =>
            {
                var openDlg = new CommonOpenFileDialog { IsFolderPicker = true };
                if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
                    await AnalyzeDirectory(openDlg.FileName);
            });

        public RelayCommand StopCommand =>
            _stopCommand ??= new(
                () => Source?.Cancel(),
                () => CanStop);

        public IAsyncCommand RefreshCommand =>
            _refreshCommand ??= new AsyncCommand(async () =>
                {
                    Source?.Cancel();
                    await AnalyzeDirectory(_rootItem.FullPath);
                },
                _ => HasRootItem);

        public static RelayCommand ExitCommand =>
            new(() => Application.Current.Shutdown());

        public RelayCommand<GridViewColumnHeader> SortCommand =>
            _sortCommand ??= new RelayCommand<GridViewColumnHeader>(Sort);

        public IAsyncCommand<ExpandLevel> ExpandCommand =>
            _expandCommand ??= new AsyncCommand<ExpandLevel>(ExpandNodesAsync,
                _ => HasRootItem);

        #endregion

        private ListCollectionView GetSelectDirectoryMenuItems()
        {
            var menuItems = new List<MenuItem>();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                var driveName = $"{drive.VolumeLabel} ({drive.Name.Remove(drive.Name.Length - 1)})";
                var command = new AsyncCommand(async () => await AnalyzeDirectory(drive.Name));
                menuItems.Add(new MenuItem { Category = DriveCategoryName, Name = driveName, Command = command });
            }

            menuItems.Add(new MenuItem
            {
                Category = DirectoryCategoryName,
                Name = "Select directory to scan",
                Command = OpenDialogCommand
            });

            var lcv = new ListCollectionView(menuItems);
            lcv.GroupDescriptions?.Add(new PropertyGroupDescription(nameof(MenuItem.Category)));
            return lcv;
        }

        private void SetUpColumnsHeaders()
        {
            NameColumnHeader = new GridViewColumnHeader { Content = NameColumnHeaderName, Command = SortCommand, Tag = nameof(NameColumnHeader) };
            NameColumnHeader.CommandParameter = NameColumnHeader;
            SizeColumnHeader = new GridViewColumnHeader { Content = SizeColumnHeaderName, Command = SortCommand, Tag = nameof(SizeColumnHeader) };
            SizeColumnHeader.CommandParameter = SizeColumnHeader;
            AllocatedColumnHeader = new GridViewColumnHeader { Content = AllocatedColumnHeaderName, Command = SortCommand, Tag = nameof(AllocatedColumnHeader) };
            AllocatedColumnHeader.CommandParameter = AllocatedColumnHeader;
            FilesColumnHeader = new GridViewColumnHeader { Content = FilesColumnHeaderName, Command = SortCommand, Tag = nameof(FilesColumnHeader) };
            FilesColumnHeader.CommandParameter = FilesColumnHeader;
            FoldersColumnHeader = new GridViewColumnHeader { Content = FoldersColumnHeaderName, Command = SortCommand, Tag = nameof(FoldersColumnHeader) };
            FoldersColumnHeader.CommandParameter = FoldersColumnHeader;
            PercentOfParentColumnHeader = new GridViewColumnHeader { Content = GetPercentOfParentColumnHeaderName(), Command = SortCommand, Tag = nameof(PercentOfParentColumnHeader) };
            PercentOfParentColumnHeader.CommandParameter = PercentOfParentColumnHeader;
            LastModifiedColumnHeader = new GridViewColumnHeader { Content = LastModifiedColumnHeaderName, Command = SortCommand, Tag = nameof(LastModifiedColumnHeader) };
            LastModifiedColumnHeader.CommandParameter = LastModifiedColumnHeader;
        }

        private string GetPercentOfParentColumnHeaderName()
        {
            return $"{PercentOfParentColumnHeaderName} ({Mode})";
        }

        private async Task AnalyzeDirectory(string directoryPath)
        {
            TreeList.Model ??= this;
            if (Source != null) await CleanUpTreeList();

            Source = new CancellationTokenSource();
            (_directoryAnalysis, _rootItem) = FileSystemItem.CreateItemAsync(directoryPath,
                Mode, Source.Token);
            HasRootItem = true;
            CanStop = true;
            TreeList.UpdateNodes();
            if (TreeList.Nodes.Count != 0)
                TreeList.Nodes[0].IsExpanded = true;
            _treeListSortColumn = null;
            Sort(AllocatedColumnHeader);
            try
            {
                await _directoryAnalysis;
            }
            catch (OperationCanceledException)
            {
            }

            UpdateStatusBarInfo();
            CanStop = false;
            Source = null;
        }

        private async Task CleanUpTreeList()
        {
            try
            {
                await _directoryAnalysis;
            }
            catch (OperationCanceledException)
            {
                _rootItem = null;
                TreeList.UpdateNodes();
            }
        }

        private void Sort(GridViewColumnHeader colHeader)
        {
            _view ??= (ListCollectionView)CollectionViewSource.GetDefaultView(TreeList.ItemsSource);

            if (_treeListSortColumn != null)
                AdornerLayer.GetAdornerLayer(_treeListSortColumn)?.Remove(_treeListSortAdorner);

            ListSortDirection newDir = ListSortDirection.Descending;
            if (_treeListSortColumn == colHeader && _treeListSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Ascending;

            _treeListSortColumn = colHeader;
            _treeListSortAdorner = new SortAdorner(_treeListSortColumn, newDir);
            AdornerLayer.GetAdornerLayer(_treeListSortColumn)?.Add(_treeListSortAdorner);
            if (_view != null) _view.CustomSort = new TreeListSorter((string)colHeader.Tag, newDir);
        }

        private void UpdateStatusBarInfo()
        {
            DriveInfo info = new DriveInfo(_rootItem.FullPath[0].ToString());
            long freeSpaceInBytes = info.AvailableFreeSpace;
            string freeSpace = ItemSizeConverter.ConvertAutomatically(freeSpaceInBytes);
            long totalSpaceInBytes = info.TotalSize;
            string totalSpace = ItemSizeConverter.ConvertAutomatically(totalSpaceInBytes);

            DiscFreeSpaceInfo = $"Free Space: {freeSpace} (of {totalSpace})";
            FilesCountInfo = $"{_rootItem.Files:N0} Files";
            ClusterSizeInfo = $"{_rootItem.ClusterSize} Bytes per Cluster ({info.DriveFormat})";
        }

        private async Task ExpandNodesAsync(ExpandLevel level)
        {
            var nodes = TreeList.Nodes;

            if (level == ExpandLevel.FullExpand)
                await ExpandAllNodesAsync(nodes);
            else
                await ExpandNodesUpToLevelAsync(nodes, (int)level + 1);
        }

        private async Task ExpandAllNodesAsync(ICollection<TreeNode> nodes)
        {
            if (nodes == null || nodes.Count == 0) return;

            await Task.Run(async () =>
            {
                foreach (TreeNode node in nodes)
                {
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = true);
                    await ExpandAllNodesAsync(node.Nodes);
                }
            });
        }

        private async Task ExpandNodesUpToLevelAsync(ICollection<TreeNode> nodes, int level)
        {
            if (level < 0) throw new ArgumentOutOfRangeException(nameof(level));

            if (nodes == null || nodes.Count == 0) return;

            if (level == 0)
            {
                foreach (TreeNode node in nodes)
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = false);
                return;
            }

            await Task.Run(async () =>
            {
                foreach (TreeNode node in nodes)
                {
                    await TreeList.Dispatcher.InvokeAsync(() => node.IsExpanded = true);
                    await ExpandNodesUpToLevelAsync(node.Nodes, level - 1);
                }
            });
        }
    }
}
