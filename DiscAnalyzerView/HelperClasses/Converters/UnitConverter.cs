using System;
using System.Globalization;
using System.Windows.Data;
using DiscAnalyzerViewModel.Enums;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(Unit), typeof(bool))]
    public class UnitConverter : IValueConverter
    {
        public static UnitConverter Instance = new();

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
