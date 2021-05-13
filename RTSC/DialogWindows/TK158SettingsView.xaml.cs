using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.DialogWindows
{
    public partial class TK158SettingsView : UserControl
    {
        public TK158SettingsView()
        {
            InitializeComponent();
        }

        private void AddressValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-1]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void BitsCountValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }     
    }
}
