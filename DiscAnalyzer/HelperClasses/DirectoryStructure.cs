using System;
using System.Collections.Generic;
using System.Linq;
using DiscAnalyzer.Models;
using System.IO;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DiscAnalyzer.HelperClasses
{
    public static class DirectoryStructure
    {
        public static List<string> GetDirectoryContents(string fullPath)
        {
            var items = new List<string>();
            try
            {
                string[] dirs = Directory.GetDirectories(fullPath);
                if (dirs.Length > 0)
                    items.AddRange(dirs);

                string[] files = Directory.GetFiles(fullPath);
                if (files.Length > 0)
                    items.AddRange(files);
            }
            catch (Exception ex)
            {
                //TODO: Add exception handler
                Console.WriteLine(ex);
                throw;
            }

            return items;
        }

        public static FileSystemItem GetFileSystemItem(string pathToItem)
        {
            try
            {
                var itemType = DefineItemType(pathToItem);

                return itemType == DirectoryItemType.File
                    ? GetFileItem(pathToItem)
                    : GetDirectoryItem(pathToItem, itemType);
            }
            catch (Exception ex)
            {
                //TODO: Add exception handler
                Console.WriteLine(ex);
                throw;
            }
        }

        private static DirectoryItemType DefineItemType(string pathToItem)
        {
            var attr = File.GetAttributes(pathToItem);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                var info = new DirectoryInfo(pathToItem);
                return info.Parent == null ? DirectoryItemType.Drive : DirectoryItemType.Folder;
            }

            return DirectoryItemType.File;
        }

        private static FileSystemItem GetFileItem(string pathToItem)
        {
            var info = new FileInfo(pathToItem);

            return new FileSystemItem
            {
                Type = DirectoryItemType.File,
                FullPath = info.FullName,
                Name = info.Name,
                Size = info.Length,
                Allocated = GetFileSizeOnDisk(info),
                Files = 1,
                Folders = 0,
                LastModified = info.LastWriteTime
            };
        }

        private static FileSystemItem GetDirectoryItem(string pathToItem, DirectoryItemType type)
        {
            var info = new DirectoryInfo(pathToItem);

            return new FileSystemItem
            {
                Type = type,
                FullPath = info.FullName,
                Name = info.Name,
                Size = GetDirectorySize(info),
                Allocated = GetDirectorySizeOnDisc(info),
                Files = info.GetFiles("*", SearchOption.AllDirectories).Length,
                Folders = info.GetDirectories("*", SearchOption.AllDirectories).Length,
                LastModified = info.LastWriteTime
            };
        }

        private static long GetDirectorySize(DirectoryInfo info)
        {
            return info.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(file => file.Length);
        }

        private static long GetFileSizeOnDisk(FileInfo info)
        {
            if (info.Directory == null) return 0;

            int result = GetDiskFreeSpaceW(info.Directory.Root.FullName, out uint sectorsPerCluster,
                out uint bytesPerSector, out _, out _);
            if (result == 0) throw new Win32Exception();

            uint clusterSize = sectorsPerCluster * bytesPerSector;
            uint losize = GetCompressedFileSizeW(info.FullName, out uint hosize);
            long size = ((long)hosize << 32) | losize;
            return (size + clusterSize - 1) / clusterSize * clusterSize;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);

        private static long GetDirectorySizeOnDisc(DirectoryInfo info)
        {
            return info.EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(GetFileSizeOnDisk);
        }
    }
}
