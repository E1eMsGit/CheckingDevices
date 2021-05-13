using System;
using System.Globalization;
using System.Windows.Data;

namespace RTSC.Devices.TA2006.Helpers
{
    class TA2006ValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)values[1])
            {
                try
                {
                    return System.Convert.ToString((byte)values[0], 2).PadLeft(8, '0');
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else if ((bool)values[2])
            {
                try
                {
                    return System.Convert.ToString((byte)values[0], 10);
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }             
            }
            else if ((bool)values[3])
            {
                try
                {
                    return System.Convert.ToString((byte)values[0], 16).ToUpper();
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
            else
            {
                return System.Convert.ToString((byte)values[0], 10);
            }
        }


        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { Binding.DoNothing, Binding.DoNothing };
        }
    }
}
