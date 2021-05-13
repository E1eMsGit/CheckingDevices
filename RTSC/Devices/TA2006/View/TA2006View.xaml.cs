using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.Devices.TA2006.View
{
    /// <summary>
    /// Логика взаимодействия для TA2006View.xaml
    /// </summary>
    public partial class TA2006View : UserControl
    {
        public TA2006View()
        {
            InitializeComponent();
        }

        public object BinRB => binRb;
        public object DecRB => decRb;
        public object HexRB => hexRb;       
    }
}
