using System;
using System.Globalization;
using System.Windows.Data;

namespace DiscAnalyzer.HelperClasses.Converters
{
    public class ItemSizeConverter : IMultiValueConverter
    {
        private const double BytesInKb = 1024;
        private const double BytesInMb = 1_048_576;
        private const double BytesInGb = 1_073_741_824;
        private const double BytesInTb = 1_099_511_627_776;

        public static ItemSizeConverter Instance = new();

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?[0] == null || value[1] == null) throw new ArgumentNullException(nameof(value));

            return (Unit)value[1] switch
            {
                Unit.Kb => $"{Math.Round((long)value[0] / BytesInKb, 1)} KB",
                Unit.Mb => $"{Math.Round((long)value[0] / BytesInMb, 1)} MB",
                Unit.Gb => $"{Math.Round((long)value[0] / BytesInGb, 1)} GB",
                _ => ConvertAutomatically(value[0])
            };
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

        public static string ConvertAutomatically(object value)
        {
            if (value is not long sizeInBytes)
                return string.Empty;
            //var sizeInBytes = (long)value;

            if (sizeInBytes > BytesInTb)
                return $"{Math.Round(sizeInBytes / BytesInTb, 1)} TB";

            if (sizeInBytes > BytesInGb)
                return $"{Math.Round(sizeInBytes / BytesInGb, 1)} GB";

            if (sizeInBytes > BytesInMb)
                return $"{Math.Round(sizeInBytes / BytesInMb, 1)} MB";

            if (sizeInBytes > BytesInKb)
                return $"{Math.Round(sizeInBytes / BytesInKb, 1)} KB";

            return $"{sizeInBytes} Bytes";
        }
    }
}
