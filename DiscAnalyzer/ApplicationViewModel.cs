using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace DiscAnalyzer
{
    public class ApplicationViewModel : INotifyPropertyChanged, ITreeModel
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
            TreeList.Model = this;
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

        private void SetUpColumnsHeaders()
        {
            NameColumnHeader = new GridViewColumnHeader { Content = "Name", Command = SortCommand, Tag = nameof(NameColumnHeader) };
            NameColumnHeader.CommandParameter = NameColumnHeader;
            SizeColumnHeader = new GridViewColumnHeader { Content = "Size", Command = SortCommand, Tag = nameof(SizeColumnHeader) };
            SizeColumnHeader.CommandParameter = SizeColumnHeader;
            AllocatedColumnHeader = new GridViewColumnHeader { Content = "Allocated", Command = SortCommand, Tag = nameof(AllocatedColumnHeader) };
            AllocatedColumnHeader.CommandParameter = AllocatedColumnHeader;
            FilesColumnHeader = new GridViewColumnHeader { Content = "Files", Command = SortCommand, Tag = nameof(FilesColumnHeader) };
            FilesColumnHeader.CommandParameter = FilesColumnHeader;
            FoldersColumnHeader = new GridViewColumnHeader { Content = "Folders", Command = SortCommand, Tag = nameof(FoldersColumnHeader) };
            FoldersColumnHeader.CommandParameter = FoldersColumnHeader;
            PercentOfParentColumnHeader = new GridViewColumnHeader { Content = "% of Parent (Allocated)", Command = SortCommand, Tag = nameof(PercentOfParentColumnHeader) };
            PercentOfParentColumnHeader.CommandParameter = PercentOfParentColumnHeader;
            LastModifiedColumnHeader = new GridViewColumnHeader { Content = "Last Modified", Command = SortCommand, Tag = nameof(LastModifiedColumnHeader) };
            LastModifiedColumnHeader.CommandParameter = LastModifiedColumnHeader;
        }
    }
}
