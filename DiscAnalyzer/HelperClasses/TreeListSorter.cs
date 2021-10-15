using System;
using System.Collections;
using System.ComponentModel;
using Aga.Controls.Tree;
using DiscAnalyzer.ViewModels;

namespace DiscAnalyzer.HelperClasses
{
    public class TreeListSorter : IComparer
    {
        private readonly string _propertyName;
        private readonly ListSortDirection _direction;

        public TreeListSorter(string propertyName, ListSortDirection direction)
        {
            _propertyName = propertyName;
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

            int result = _propertyName switch
            {
                "Name" => string.Compare(itemY.Name, itemX.Name, StringComparison.Ordinal),
                "Size" => itemY.Size.CompareTo(itemX.Size),
                "Allocated" => itemY.Allocated.CompareTo(itemX.Allocated),
                "Files" => itemY.Files.CompareTo(itemX.Files),
                "Folders" => itemY.Folders.CompareTo(itemX.Folders),
                "PercentOfParent" => itemY.PercentOfParent.CompareTo(itemX.PercentOfParent),
                "LastModified" => itemY.LastModified.CompareTo(itemX.LastModified),
                _ => throw new ArgumentException("Invalid property name")
            };

            return _direction == ListSortDirection.Ascending ? result * -1 : result;
        }
    }
}
