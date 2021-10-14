using System;

namespace DiscAnalyzer.Models
{
    public class FileSystemItem
    {
        public DirectoryItemType Type { get; set; }

        public string FullPath { get; set; }

        public string Name { get; set; }

        public long Size { get; set; }

        public long Allocated { get; set; }

        public int Files { get; set; }

        public int Folders { get; set; }

        public DateTime LastModified { get; set; }
    }
}
