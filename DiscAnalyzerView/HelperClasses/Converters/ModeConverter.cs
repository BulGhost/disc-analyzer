using System;
using System.Globalization;
using System.Windows.Data;
using DiscAnalyzerModel.Enums;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(ItemBaseProperty), typeof(bool))]
    public class ModeConverter : IValueConverter
    {
        public static ModeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true
                ? parameter
                : Binding.DoNothing;
        }
    }
}
