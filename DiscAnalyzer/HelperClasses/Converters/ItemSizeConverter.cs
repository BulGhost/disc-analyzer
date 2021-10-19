using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(long), typeof(string))]
    public class ItemSizeConverter : IValueConverter
    {
        public static ItemSizeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            return ((string) parameter)?.ToUpper() switch
            {
                "KB" => $"{Math.Round((double) value / 1000, 1)} KB",
                "MB" => $"{Math.Round((double) value / 1000_000, 1)} MB",
                "GB" => $"{Math.Round((double) value / 1000_000_000, 1)} GB",
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

            if (sizeInBytes > 1000_000_000_000)
                return $"{Math.Round((double)sizeInBytes / 1000_000_000_000, 1)} TB";

            if (sizeInBytes > 1000_000_000)
                return $"{Math.Round((double)sizeInBytes / 1000_000_000, 1)} GB";

            if (sizeInBytes > 1000_000)
                return $"{Math.Round((double)sizeInBytes / 1000_000, 1)} MB";

            if (sizeInBytes > 1000)
                return $"{Math.Round((double)sizeInBytes / 1000, 1)} KB";

            return $"{sizeInBytes} Bytes";
        }
    }
}
