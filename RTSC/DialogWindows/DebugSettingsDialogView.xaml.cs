using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.DialogWindows
{
    /// <summary>
    /// Логика взаимодействия для DebugSettingsDialogView.xaml
    /// </summary>
    public partial class DebugSettingsDialogView : UserControl
    {
        public DebugSettingsDialogView()
        {
            InitializeComponent();
        }

        private void TimerValueValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
