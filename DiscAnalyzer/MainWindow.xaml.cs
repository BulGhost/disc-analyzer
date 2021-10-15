using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using Aga.Controls.Tree;
using DiscAnalyzer.HelperClasses;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        private GridViewColumnHeader _listViewSortCol;
        private SortAdorner _listViewSortAdorner;
        private ListCollectionView _view;

        public MainWindow()
        {
            InitializeComponent();
        }

        public ApplicationViewModel ViewModel { get; set; } = new();

        public TreeList XProp => Tree;

        private void TreeColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = sender as GridViewColumnHeader;
            string sortBy = column.Tag.ToString();
            _view ??= (ListCollectionView)CollectionViewSource.GetDefaultView(Tree.ItemsSource);

            if (_listViewSortCol != null)
                AdornerLayer.GetAdornerLayer(_listViewSortCol)?.Remove(_listViewSortAdorner);

            ListSortDirection newDir = ListSortDirection.Descending;
            if (_listViewSortCol == column && _listViewSortAdorner.Direction == newDir)
                newDir = ListSortDirection.Ascending;

            _listViewSortCol = column;
            _listViewSortAdorner = new SortAdorner(_listViewSortCol, newDir);
            AdornerLayer.GetAdornerLayer(_listViewSortCol)?.Add(_listViewSortAdorner);
            _view.CustomSort = new TreeListSorter(sortBy, newDir);
        }
    }
}
