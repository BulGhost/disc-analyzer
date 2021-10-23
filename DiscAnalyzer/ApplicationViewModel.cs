using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Aga.Controls.Tree;
using DiscAnalyzer.Commands;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.Models;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DiscAnalyzer
{
    public class ApplicationViewModel : INotifyPropertyChanged, ITreeModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private FileSystemItem _rootItem;
        private GridViewColumnHeader _treeListSortColumn;
        private SortAdorner _treeListSortAdorner;
        private ListCollectionView _view;
        private RelayCommand _openDialogCommand;
        private RelayCommand<GridViewColumnHeader> _sortCommand;

        public TreeList TreeList { get; }
        public GridViewColumnHeader NameColumnHeader { get; set; }
        public GridViewColumnHeader SizeColumnHeader { get; set; }
        public GridViewColumnHeader AllocatedColumnHeader { get; set; }
        public GridViewColumnHeader FilesColumnHeader { get; set; }
        public GridViewColumnHeader FoldersColumnHeader { get; set; }
        public GridViewColumnHeader PercentOfParentColumnHeader { get; set; }
        public GridViewColumnHeader LastModifiedColumnHeader { get; set; }

        public ApplicationViewModel(TreeList treeList)
        {
            TreeList = treeList;
            SetUpColumnsHeaders();
        }

        public IEnumerable GetChildren(object parent)
        {
            return parent == null
                ? new ObservableCollection<FileSystemItem> { _rootItem }
                : (parent as FileSystemItem)?.Children;
        }

        public bool HasChildren(object parent)
        {
            return parent is FileSystemItem item && (item.Children == null || item.Children.Count > 0);
        }

        public RelayCommand OpenDialogCommand =>
            _openDialogCommand ??= new RelayCommand(async () =>
            {
                var openDlg = new CommonOpenFileDialog { IsFolderPicker = true };
                if (openDlg.ShowDialog() == CommonFileDialogResult.Ok)
                    await AnalyzeDirectory(openDlg.FileName);
            });

        public RelayCommand<GridViewColumnHeader> SortCommand =>
            _sortCommand ??= new RelayCommand<GridViewColumnHeader>(Sort);

        public RelayCommand RefreshCommand =>
            new(async () => await AnalyzeDirectory(_rootItem.FullPath),
                () => _rootItem != null);

        public RelayCommand ExitCommand =>
            new(() => Application.Current.Shutdown());

        private async Task AnalyzeDirectory(string directoryPath)
        {
            //await Task.Run(() => _rootItem = new FileSystemItem(directoryPath, true))
            //    .ConfigureAwait(false);
            Task directoryAnalysis = Task.Run(() => _rootItem = new FileSystemItem(directoryPath, true));

            await TreeList.Dispatcher.InvokeAsync(async () =>
            {
                TreeList.Model ??= this;
                do
                {
                    TreeList.UpdateNodes();
                    if (TreeList.Nodes.Count != 0)
                        TreeList.Nodes[0].IsExpanded = true;
                    //_treeListSortColumn = null;
                    //Sort(AllocatedColumnHeader);
                    await Task.Delay(1000);
                } while (true);
            });
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
