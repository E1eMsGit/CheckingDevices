using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RTSC.Devices.Helpers
{
    class ByteToBitsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return value;
            }
            else if (System.Convert.ToInt32(parameter) == 8)
            {
                return System.Convert.ToString((byte)value, 2).PadLeft(8, '0');
            }
            else
            {
                return System.Convert.ToString((byte)value, 2).PadLeft(4, '0');
            }          
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }
}
