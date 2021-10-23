using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(long), typeof(string))]
    public class ItemSizeConverter : IValueConverter
    {
        private const double BytesInKb = 1024;
        private const double BytesInMb = 1_048_576;
        private const double BytesInGb = 1_073_741_824;
        private const double BytesInTb = 1_099_511_627_776;

        public static ItemSizeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return ((string)parameter)?.ToUpper() switch
            {
                "KB" => $"{Math.Round((double)value / BytesInKb, 1)} KB",
                "MB" => $"{Math.Round((double)value / BytesInMb, 1)} MB",
                "GB" => $"{Math.Round((double)value / BytesInGb, 1)} GB",
                _ => ConvertAutomatically(value)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }

        private string ConvertAutomatically(object value)
        {
            var sizeInBytes = (long)value;

            if (sizeInBytes > BytesInTb)
                return $"{Math.Round((double)sizeInBytes / BytesInTb, 1)} TB";

            if (sizeInBytes > BytesInGb)
                return $"{Math.Round((double)sizeInBytes / BytesInGb, 1)} GB";

            if (sizeInBytes > BytesInMb)
                return $"{Math.Round((double)sizeInBytes / BytesInMb, 1)} MB";

            if (sizeInBytes > BytesInKb)
                return $"{Math.Round((double)sizeInBytes / BytesInKb, 1)} KB";

            return $"{sizeInBytes} Bytes";
        }
    }
}