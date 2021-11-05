using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using DiscAnalyzer.HelperClasses;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzer
{
    public partial class MainWindow : RibbonWindow
    {
        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Descending;
        private ListCollectionView _dataView;

        public MainWindow(ILogger<MainWindow> logger)
        {
            try
            {
                InitializeComponent();

                DataContext = new ApplicationViewModel(Tree, logger);
                SetInitialSortingSettings(AllocatedColumn, ListSortDirection.Descending);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during {0} constructing", nameof(MainWindow));
                throw;
            }
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader headerClicked ||
                headerClicked.Role == GridViewColumnHeaderRole.Padding) return;

            ListSortDirection direction = SetDirection(headerClicked);

            var sortBy = (TreeListViewColumn)headerClicked.Tag;
            _dataView.CustomSort = new TreeListSorter(sortBy, direction);

            if (direction == ListSortDirection.Ascending)
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            else
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

            if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                _lastHeaderClicked.Column.HeaderTemplate = null;

            _lastHeaderClicked = headerClicked;
            _lastDirection = direction;
        }

        private ListSortDirection SetDirection(GridViewColumnHeader headerClicked)
        {
            if (headerClicked != _lastHeaderClicked && headerClicked.Column != NameColumn)
                return headerClicked.Column != NameColumn
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;

            return _lastDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
        }

        private void SetInitialSortingSettings(GridViewColumn column, ListSortDirection direction)
        {
            _dataView = (ListCollectionView)CollectionViewSource.GetDefaultView(Tree.ItemsSource);
            var columnHeader = (GridViewColumnHeader)AllocatedColumn.Header;
            var sortBy = (TreeListViewColumn)columnHeader.Tag;
            _dataView.CustomSort = new TreeListSorter(sortBy, direction);

            column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;
        }
    }
}
