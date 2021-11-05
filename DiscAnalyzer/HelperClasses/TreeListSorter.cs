using System;
using System.Collections;
using System.ComponentModel;
using Aga.Controls.Tree;

namespace DiscAnalyzer.HelperClasses
{
    public class TreeListSorter : IComparer
    {
        private readonly TreeListViewColumn _column;
        private readonly ListSortDirection _direction;

        public TreeListSorter(TreeListViewColumn column, ListSortDirection direction)
        {
            _column = column;
            _direction = direction;
        }

        public int Compare(object x, object y)
        {
            if (x is not TreeNode nodeX || y is not TreeNode nodeY) return 0;

            if (nodeX.Level == -1 || nodeY.Level == -1) return 0;

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
            var itemX = (FileSystemItem)nodeX.Tag;
            var itemY = (FileSystemItem)nodeY.Tag;

            int result = _column switch
            {
                TreeListViewColumn.Name => string.Compare(itemY.Name, itemX.Name, StringComparison.OrdinalIgnoreCase),
                TreeListViewColumn.Size => itemY.Size.CompareTo(itemX.Size),
                TreeListViewColumn.Allocated => itemY.Allocated.CompareTo(itemX.Allocated),
                TreeListViewColumn.Files => itemY.Files.CompareTo(itemX.Files),
                TreeListViewColumn.Folders => itemY.Folders.CompareTo(itemX.Folders),
                TreeListViewColumn.PercentOfParent => itemY.PercentOfParent.CompareTo(itemX.PercentOfParent),
                TreeListViewColumn.LastModified => itemY.LastModified.CompareTo(itemX.LastModified),
                _ => throw new ArgumentException("Invalid property name")
            };

            return _direction == ListSortDirection.Ascending ? result * -1 : result;
        }
    }
}
