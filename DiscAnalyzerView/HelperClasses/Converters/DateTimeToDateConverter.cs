using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(DateTime), typeof(string))]
    public class DateTimeToDateConverter : IValueConverter
    {
        public static DateTimeToDateConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null
                ? string.Empty
                : ((DateTime)value).ToShortDateString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
