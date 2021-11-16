using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace DiscAnalyzerModel.HelperClasses
{
    public class FileSizeOnDiskDeterminator
    {
        private string _driveName;
        private uint _clusterSize;

        public long GetFileSizeOnDisk(FileInfo info)
        {
            if (DriveNameChanged(info.FullName, out string newDriveName))
            {
                _driveName = newDriveName;
                _clusterSize = DetermineClusterSize(newDriveName);
            }

            uint losize = GetCompressedFileSizeW(info.FullName, out uint hosize);
            long size = ((long)hosize << 32) | losize;
            return (size + _clusterSize - 1) / _clusterSize * _clusterSize;
        }

        public static uint DetermineClusterSize(string path)
        {
            if (!(Directory.Exists(path) || File.Exists(path)))
            {
                throw new ArgumentException("File or directory with such path doesn't exist", nameof(path));
            }

            int result = GetDiskFreeSpaceW(path, out uint sectorsPerCluster,
                out uint bytesPerSector, out _, out _);
            if (result == 0)
            {
                throw new Win32Exception();
            }

            return sectorsPerCluster * bytesPerSector;
        }

        private bool DriveNameChanged(string fileFullName, out string newDriveName)
        {
            newDriveName = Path.GetPathRoot(fileFullName);
            return newDriveName?.ToUpperInvariant() != _driveName;
        }

        [DllImport("kernel32.dll")]
        private static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        private static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);
    }
}
