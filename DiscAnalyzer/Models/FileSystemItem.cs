using System;
//using DiscAnalyzer.HelperClasses;

namespace DiscAnalyzer.Models
{
    public class FileSystemItem
    {
        public DirectoryItemType Type { get; set; }

        public string FullPath { get; set; }

        //public string Name => Type == DirectoryItemType.Drive
        //    ? FullPath
        //    : DirectoryStructure.GetFileFolderName(FullPath);

        public string Name { get; set; }

        public long Size { get; set; }

        public long Allocated { get; set; }

        public int Files { get; set; }

        public int Folders { get; set; }

        //public int PercentOfParent { get; set; }

        public DateTime LastModified { get; set; }
    }
}
