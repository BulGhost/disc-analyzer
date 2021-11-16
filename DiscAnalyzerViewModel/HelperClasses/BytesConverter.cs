using System;
using DiscAnalyzerViewModel.Resourses;

namespace DiscAnalyzerViewModel.HelperClasses
{
    public static class BytesConverter
    {
        public const double BytesInKb = 1024;
        public const double BytesInMb = 1_048_576;
        public const double BytesInGb = 1_073_741_824;
        public const double BytesInTb = 1_099_511_627_776;

        public static string ConvertAutomatically(long sizeInBytes)
        {
            if (sizeInBytes > BytesInTb)
            {
                return string.Format(Resources.SizeInTb, Math.Round(sizeInBytes / BytesInTb, 1));
            }

            if (sizeInBytes > BytesInGb)
            {
                return string.Format(Resources.SizeInGb, Math.Round(sizeInBytes / BytesInGb, 1));
            }

            if (sizeInBytes > BytesInMb)
            {
                return string.Format(Resources.SizeInMb, Math.Round(sizeInBytes / BytesInMb, 1));
            }

            if (sizeInBytes > BytesInKb)
            {
                return string.Format(Resources.SizeInKb, Math.Round(sizeInBytes / BytesInKb, 1));
            }

            return string.Format(Resources.SizeInBytes, sizeInBytes);
        }
    }
}
