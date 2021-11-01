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
using DiscAnalyzer.Models;
using Microsoft.WindowsAPICodePack.Dialogs;
using MenuItem = DiscAnalyzer.HelperClasses.MenuItem;

namespace DiscAnalyzer
{
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
        private const string PercentOfParentColumnHeaderName = "% of Parent (Allocated)";
        private const string LastModifiedColumnHeaderName = "Last Modified";

        #endregion

        #region Fields

        private FileSystemItem _rootItem;
        private GridViewColumnHeader _treeListSortColumn;
        private SortAdorner _treeListSortAdorner;
        private ListCollectionView _view;
        private IAsyncCommand _openDialogCommand;
        private IAsyncCommand _refreshCommand;
        private bool _canRefresh;
        private RelayCommand<GridViewColumnHeader> _sortCommand;
        private Task _directoryAnalysis;

        #endregion

        #region Properties

        public TreeList TreeList { get; }
        public ListCollectionView SelectDirectoryMenuItems { get; }
        public GridViewColumnHeader NameColumnHeader { get; set; }
        public GridViewColumnHeader SizeColumnHeader { get; set; }
        public GridViewColumnHeader AllocatedColumnHeader { get; set; }
        public GridViewColumnHeader FilesColumnHeader { get; set; }
        public GridViewColumnHeader FoldersColumnHeader { get; set; }
        public GridViewColumnHeader PercentOfParentColumnHeader { get; set; }
        public GridViewColumnHeader LastModifiedColumnHeader { get; set; }
        public CancellationTokenSource Source { get; set; }
        public bool CanStop { get; set; }

        public bool CanRefresh
        {
            get => _canRefresh;
            set
            {
                if (_canRefresh != value)
                {
                    _canRefresh = value;
                    RefreshCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion

        public ApplicationViewModel(TreeList treeList)
        {
            TreeList = treeList;
            SelectDirectoryMenuItems = GetSelectDirectoryMenuItems();
            SetUpColumnsHeaders();
        }

        private ListCollectionView GetSelectDirectoryMenuItems()
        {
            var menuItems = new List<MenuItem>();
            var drives = DriveInfo.GetDrives();
            foreach (var drive in drives)
            {
                var driveName = $"{drive.VolumeLabel} ({drive.Name.Remove(drive.Name.Length - 1)})";
                var command = new AsyncCommand(async () => await AnalyzeDirectory(drive.Name));
                menuItems.Add(new MenuItem {Category = DriveCategoryName, Name = driveName, Command = command});
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
            PercentOfParentColumnHeader = new GridViewColumnHeader { Content = PercentOfParentColumnHeaderName, Command = SortCommand, Tag = nameof(PercentOfParentColumnHeader) };
            PercentOfParentColumnHeader.CommandParameter = PercentOfParentColumnHeader;
            LastModifiedColumnHeader = new GridViewColumnHeader { Content = LastModifiedColumnHeaderName, Command = SortCommand, Tag = nameof(LastModifiedColumnHeader) };
            LastModifiedColumnHeader.CommandParameter = LastModifiedColumnHeader;
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

        public IAsyncCommand OpenDialogCommand =>
            _openDialogCommand ??= new AsyncCommand(async () =>
            {
                var openDlg = new CommonOpenFileDialog { IsFolderPicker = true };
                if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
                    await AnalyzeDirectory(openDlg.FileName);
            });

        public RelayCommand<GridViewColumnHeader> SortCommand =>
            _sortCommand ??= new RelayCommand<GridViewColumnHeader>(Sort);

        public RelayCommand StopCommand =>
            new(() => Source?.Cancel(),
                () => CanStop);

        public IAsyncCommand RefreshCommand =>
            _refreshCommand ??= new AsyncCommand(async () =>
                {
                    Source?.Cancel();
                    await AnalyzeDirectory(_rootItem.FullPath);
                },
                _ => CanRefresh);

        public static RelayCommand ExitCommand =>
            new(() => Application.Current.Shutdown());

        private async Task AnalyzeDirectory(string directoryPath)
        {
            TreeList.Model ??= this;
            if (Source != null) await CleanUpTreeList();

            Source = new CancellationTokenSource();
            (_directoryAnalysis, _rootItem) = FileSystemItem.CreateItemAsync(directoryPath, Source.Token);
            CanRefresh = true;
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
    }
}
