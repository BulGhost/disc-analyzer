using System;
using System.Globalization;
using System.Windows.Data;
using DiscAnalyzerView.Enums;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(ItemProperty), typeof(bool))]
    public class ModeConverter : IValueConverter
    {
        public static ModeConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
