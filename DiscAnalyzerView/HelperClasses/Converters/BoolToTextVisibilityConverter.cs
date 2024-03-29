﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzerView.HelperClasses.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToTextVisibilityConverter : IValueConverter
    {
        public static BoolToTextVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool)value
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
