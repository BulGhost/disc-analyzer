using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using DiscAnalyzerView.HelperClasses;
using DiscAnalyzerViewModel;
using Microsoft.Extensions.Logging;

namespace DiscAnalyzerView
{
    public partial class MainWindow : RibbonWindow
    {
        private GridViewColumnHeader _lastHeaderClicked;
        private ListSortDirection _lastDirection = ListSortDirection.Descending;
        private readonly ListCollectionView _dataView;

        public MainWindow(ILogger<MainWindow> logger)
        {
            try
            {
                InitializeComponent();

                DataContext = new ApplicationViewModel(Tree, logger);
                _dataView = (ListCollectionView)CollectionViewSource.GetDefaultView(Tree.ItemsSource);
                ApplySorting(NameColumn, ListSortDirection.Ascending);
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

            ApplySorting(headerClicked.Column, direction);
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

        private void ApplySorting(GridViewColumn column, ListSortDirection direction)
        {
            var columnHeader = (GridViewColumnHeader)column.Header;
            var sortBy = (TreeListViewColumn)columnHeader.Tag;
            _dataView.CustomSort = new TreeListSorter(sortBy, direction);

            if (direction == ListSortDirection.Ascending)
                column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;
            else
                column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;

            if (_lastHeaderClicked != null && _lastHeaderClicked != columnHeader)
                _lastHeaderClicked.Column.HeaderTemplate = null;

            _lastHeaderClicked = columnHeader;
            _lastDirection = direction;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if ((RibbonApplicationMenu.Template.FindName("MainPaneBorder", RibbonApplicationMenu) as Border)?.Parent is Grid grid)
            {
                grid.ColumnDefinitions[2].Width = new GridLength(0);
            }
        }
    }
}
