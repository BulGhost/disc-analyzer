using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(int), typeof(double))]
    public class PercentToOffsetConverter : IValueConverter
    {
        public static PercentToOffsetConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? 0 : (double)(int) value / 1000;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
