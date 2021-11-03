using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(string), typeof(BitmapImage))]
    public class CategoryToImageConverter : IValueConverter
    {
        public static CategoryToImageConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string image = (string)value switch
            {
                ApplicationViewModel.DriveCategoryName => "Images/TreeIcons/drive.png",
                ApplicationViewModel.DirectoryCategoryName => "Images/MenuIcons/open-64.png",
                _ => string.Empty
            };

            return new BitmapImage(new Uri($"pack://application:,,,/{image}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
