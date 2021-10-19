﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class HasItemsToVisibilityConverter : IValueConverter
    {
        public static HasItemsToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}