﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace NINA.Plugin.TargetScheduler.Controls.Converters {

    public class AltitudeChoicesConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            double d = (double)value;
            return $"{d}°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value == null) {
                return 0;
            }

            string s = (string)value;
            return double.Parse(s.TrimEnd('°'));
        }
    }
}