using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace RTSC.Devices.Helpers
{
    class Constants
    {
        public static readonly byte PHASING = 0x08;
        public static readonly List<byte> LIST_ADDRESSES_COUNT = Enumerable.Range(0, 15).Select(x => Convert.ToByte(x)).ToList();

        public static readonly SolidColorBrush LIGHT_THEME_COLOR = Application.Current.TryFindResource("PrimaryHueLightBrush") as SolidColorBrush;
        public static readonly SolidColorBrush MID_THEME_COLOR = Application.Current.TryFindResource("PrimaryHueMidBrush") as SolidColorBrush;
        public static readonly SolidColorBrush DARK_THEME_COLOR = Application.Current.TryFindResource("PrimaryHueDarkBrush") as SolidColorBrush;
        public static readonly SolidColorBrush ACCENT_THEME_COLOR = Application.Current.TryFindResource("SecondaryHueMidBrush") as SolidColorBrush;
        public static readonly SolidColorBrush BODY_THEME_COLOR = Application.Current.TryFindResource("MaterialDesignBody") as SolidColorBrush;
        public static readonly SolidColorBrush GREEN_COLOR = new SolidColorBrush(Colors.Green);
        public static readonly SolidColorBrush RED_COLOR = new SolidColorBrush(Colors.Red);
        public static readonly SolidColorBrush WHITE_COLOR = new SolidColorBrush(Colors.White);
    }
}
 