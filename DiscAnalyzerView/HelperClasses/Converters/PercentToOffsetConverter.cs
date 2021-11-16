using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(int), typeof(double))]
    public class PercentToOffsetConverter : IValueConverter
    {
        private const int _maxPercent = 1000;
        public static PercentToOffsetConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null
                ? 0
                : (double)(int) value / _maxPercent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
