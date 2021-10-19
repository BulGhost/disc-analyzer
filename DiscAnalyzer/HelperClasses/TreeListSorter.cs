using System;
using System.Collections;
using System.ComponentModel;
using Aga.Controls.Tree;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer.HelperClasses
{
    public class TreeListSorter : IComparer
    {
        private readonly string _columnHeader;
        private readonly ListSortDirection _direction;

        public TreeListSorter(string columnHeader, ListSortDirection direction)
        {
            _columnHeader = columnHeader;
            _direction = direction;
        }

        public int Compare(object x, object y)
        {
            if (x is not TreeNode nodeX || y is not TreeNode nodeY) return 0;

            if (nodeY.Parent == nodeX.Parent)
                return CompareNodesWithCommonParent(nodeX, nodeY);

            if (nodeX.Level == nodeY.Level)
                return Compare(nodeX.Parent, nodeY.Parent);

            if (nodeX.Level < nodeY.Level)
                return nodeX == nodeY.Parent ? -1 : Compare(nodeX, nodeY.Parent);

            return nodeX.Parent == nodeY ? 1 : Compare(nodeX.Parent, nodeY);
        }

        private int CompareNodesWithCommonParent(TreeNode nodeX, TreeNode nodeY)
        {
            var itemX = (FileSystemItemViewModel)nodeX.Tag;
            var itemY = (FileSystemItemViewModel)nodeY.Tag;

            int result = _columnHeader switch
            {
                nameof(ApplicationViewModel.NameColumnHeader) => string.Compare(itemY.Name, itemX.Name, StringComparison.OrdinalIgnoreCase),
                nameof(ApplicationViewModel.SizeColumnHeader) => itemY.Size.CompareTo(itemX.Size),
                nameof(ApplicationViewModel.AllocatedColumnHeader) => itemY.Allocated.CompareTo(itemX.Allocated),
                nameof(ApplicationViewModel.FilesColumnHeader) => itemY.Files.CompareTo(itemX.Files),
                nameof(ApplicationViewModel.FoldersColumnHeader) => itemY.Folders.CompareTo(itemX.Folders),
                nameof(ApplicationViewModel.PercentOfParentColumnHeader) => itemY.PercentOfParent.CompareTo(itemX.PercentOfParent),
                nameof(ApplicationViewModel.LastModifiedColumnHeader) => itemY.LastModified.CompareTo(itemX.LastModified),
                _ => throw new ArgumentException("Invalid property name")
            };

            return _direction == ListSortDirection.Ascending ? result * -1 : result;
        }
    }
}
