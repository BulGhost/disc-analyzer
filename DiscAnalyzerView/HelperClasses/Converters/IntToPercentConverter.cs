using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(int), typeof(string))]
    public class IntToPercentConverter : IValueConverter
    {
        public static IntToPercentConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null
                ? string.Empty
                : $"{(double)(int)value / 10} %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
