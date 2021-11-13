using System;
using System.Globalization;
using System.Windows.Data;
using DiscAnalyzerViewModel.Enums;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    public class ItemSizeConverter : IMultiValueConverter
    {
        private const double _bytesInKb = 1024;
        private const double _bytesInMb = 1_048_576;
        private const double _bytesInGb = 1_073_741_824;
        private const double _bytesInTb = 1_099_511_627_776;

        public static ItemSizeConverter Instance = new();

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?[0] == null || value[1] == null) throw new ArgumentNullException(nameof(value));

            if (value[0] is not long sizeInBytes) return string.Empty;

            return (Unit)value[1] switch
            {
                Unit.Kb => $"{Math.Round((long)value[0] / _bytesInKb, 1)} KB",
                Unit.Mb => $"{Math.Round((long)value[0] / _bytesInMb, 1)} MB",
                Unit.Gb => $"{Math.Round((long)value[0] / _bytesInGb, 1)} GB",
                _ => ConvertAutomatically(sizeInBytes)
            };
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string ConvertAutomatically(long sizeInBytes) //TODO: Delete double
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
