using System;

namespace DiscAnalyzerViewModel.HelperClasses
{
    public class BytesConverter
    {
        private const double _bytesInKb = 1024;
        private const double _bytesInMb = 1_048_576;
        private const double _bytesInGb = 1_073_741_824;
        private const double _bytesInTb = 1_099_511_627_776;

        public static string ConvertAutomatically(long sizeInBytes)
        {
            if (sizeInBytes > _bytesInTb)
                return $"{Math.Round(sizeInBytes / _bytesInTb, 1)} TB";

            if (sizeInBytes > _bytesInGb)
                return $"{Math.Round(sizeInBytes / _bytesInGb, 1)} GB";

            if (sizeInBytes > _bytesInMb)
                return $"{Math.Round(sizeInBytes / _bytesInMb, 1)} MB";

            if (sizeInBytes > _bytesInKb)
                return $"{Math.Round(sizeInBytes / _bytesInKb, 1)} KB";

            return $"{sizeInBytes} Bytes";
        }
    }
}
