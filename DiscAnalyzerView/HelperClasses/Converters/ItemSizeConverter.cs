using System;
using System.Globalization;
using System.Windows.Data;
using DiscAnalyzerViewModel.Enums;
using static DiscAnalyzerViewModel.Resourses.Resources;
using static DiscAnalyzerViewModel.HelperClasses.BytesConverter;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    public class ItemSizeConverter : IMultiValueConverter
    {
        public static ItemSizeConverter Instance = new();

        public object Convert(object[] value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?[0] == null || value[1] == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (value[0] is not long sizeInBytes)
            {
                return string.Empty;
            }

            return (Unit)value[1] switch
            {
                Unit.Kb => string.Format(SizeInKb, Math.Round((long)value[0] / BytesInKb, 1)),
                Unit.Mb => string.Format(SizeInMb, Math.Round((long)value[0] / BytesInMb, 1)),
                Unit.Gb => string.Format(SizeInGb, Math.Round((long)value[0] / BytesInGb, 1)),
                _ => ConvertAutomatically(sizeInBytes)
            };
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
