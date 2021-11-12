﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using DiscAnalyzer.Enums;

namespace DiscAnalyzer.HelperClasses.Converters
{
    [ValueConversion(typeof(ItemProperty), typeof(Visibility))]
    public class ModeToVisibilityConverter : IValueConverter
    {
        public static ModeToVisibilityConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null && value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
