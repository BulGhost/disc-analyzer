using System;
using System.Collections.Generic;
using System.Linq;
using DiscAnalyzer.Models;
using System.IO;

namespace DiscAnalyzer.HelperClasses
{
    public static class DirectoryStructure
    {
        public static List<DirectoryItem> GetLogicalDrives()
        {
            return Directory.GetLogicalDrives().Select(drive =>
                new DirectoryItem {FullPath = drive, Type = DirectoryItemType.Drive}).ToList();
        }

        public static List<DirectoryItem> GetDirectoryContents(string fullPath)
        {
            var items = new List<DirectoryItem>();
            try
            {
                var dirs = Directory.GetDirectories(fullPath);
                if (dirs.Length > 0) items.AddRange(dirs.Select(dir =>
                    new DirectoryItem {FullPath = dir, Type = DirectoryItemType.Folder}));
            }
            catch (Exception ex)
            {
                //TODO: Add exception handler
            }

            try
            {
                var fs = Directory.GetFiles(fullPath);
                if (fs.Length > 0) items.AddRange(fs.Select(file =>
                    new DirectoryItem {FullPath = file, Type = DirectoryItemType.File}));
            }
            catch (Exception ex)
            {
                //TODO: Add exception handler
            }

            return items;
        }

        public static string GetFileFolderName(string path)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;

            var normalizedPath = path.Replace('/', '\\');
            var lastIndex = normalizedPath.LastIndexOf('\\');

            return lastIndex <= 0 ? path : path.Substring(lastIndex + 1);
        }
    }
}
