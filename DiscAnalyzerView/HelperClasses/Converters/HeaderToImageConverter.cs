using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(DirectoryItemType), typeof(BitmapImage))]
    public class HeaderToImageConverter : IValueConverter
    {
        public static HeaderToImageConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var image = "Images/TreeIcons/file.png";

            if (value != null)
                image = (DirectoryItemType) value switch
                {
                    DirectoryItemType.Drive => "Images/TreeIcons/drive.png",
                    DirectoryItemType.Folder => "Images/TreeIcons/folder-closed.png",
                    _ => image
                };

            return new BitmapImage(new Uri($"pack://application:,,,/{image}"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
