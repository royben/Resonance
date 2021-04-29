﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Resonance.Examples.Common.Converters
{
    /// <summary>
    /// Inversed binding converter for standard boolean to visibility and back.
    /// </summary>
    public class BooleanToVisibilityInverseConverter : IValueConverter
    {
        /// <summary>
        /// Converts a boolean value to visibility enumeration.
        /// </summary>
        /// <param name="value">Boolean value.</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value) return Visibility.Collapsed;
            return Visibility.Visible;
        }

        /// <summary>
        /// Converts a visibility enumeration to boolean value.
        /// </summary>
        /// <param name="value">Visibility enumeration</param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((Visibility)value == Visibility.Visible) return false;
            else return true;
        }
    }
}
