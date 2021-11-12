using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

namespace DiscAnalyzerView.HelperClasses
{
    public class FileSizeOnDiskDeterminationHelper
    {
        public uint GetClusterSize(string path)
        {
            if (!(Directory.Exists(path) || File.Exists(path)))
            {
                throw new ArgumentException("File or directory with such path doesn't exist", nameof(path));
            }

            int result = GetDiskFreeSpaceW(path, out uint sectorsPerCluster,
                out uint bytesPerSector, out _, out _);
            if (result == 0) throw new Win32Exception();

            return sectorsPerCluster * bytesPerSector;
        }

        [DllImport("kernel32.dll")]
        public static extern uint GetCompressedFileSizeW([In, MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
            [Out, MarshalAs(UnmanagedType.U4)] out uint lpFileSizeHigh);

        [DllImport("kernel32.dll", SetLastError = true, PreserveSig = true)]
        public static extern int GetDiskFreeSpaceW([In, MarshalAs(UnmanagedType.LPWStr)] string lpRootPathName,
            out uint lpSectorsPerCluster, out uint lpBytesPerSector, out uint lpNumberOfFreeClusters,
            out uint lpTotalNumberOfClusters);
    }
}
