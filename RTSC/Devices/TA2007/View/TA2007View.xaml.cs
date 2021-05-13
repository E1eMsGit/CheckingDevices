using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.Devices.TA2007.View
{
    /// <summary>
    /// Логика взаимодействия для TA2007View.xaml
    /// </summary>
    public partial class TA2007View : UserControl
    {
        public TA2007View()
        {
            InitializeComponent();
        }

        private void ValueValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            var decimalSeparator = ci.NumberFormat.NumberDecimalSeparator;
            var floatRegex = string.Format("^[-]?[0-9]*[.]?$", decimalSeparator);

            Regex regex = new Regex(floatRegex);
            e.Handled = !regex.IsMatch(e.Text);
        }
    }
}
