using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    class DateTimeToDateConverter : IValueConverter
    {
        public static DateTimeToDateConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            return ((DateTime)value).ToShortDateString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
