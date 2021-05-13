using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTSC.Devices.TA1004M1.View
{
    /// <summary>
    /// Логика взаимодействия для TA1004M1.xaml
    /// </summary>
    public partial class TA1004M1View : UserControl
    {
        private TextBlock[] _tbs;

        public TA1004M1View()
        {
            InitializeComponent();

            _tbs = new TextBlock[] { tbPassingInformation, tbOutputInformation, tbCodeBits };

            AddHandler(RadioButton.CheckedEvent, new RoutedEventHandler(RbChecked));
            AddHandler(RadioButton.UncheckedEvent, new RoutedEventHandler(RbUnchecked));
        }

        private void AddressPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-1]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void RbChecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = e.OriginalSource as RadioButton;

            for (int i = 0; i < _tbs.Length; i++)
            {
                if (rb.Content.ToString() == _tbs[i].Text)
                {
                    _tbs[i].Foreground = Devices.Helpers.Constants.WHITE_COLOR;
                    DependencyObject parent = LogicalTreeHelper.GetParent(_tbs[i]);
                    ((Control)parent).Background = Devices.Helpers.Constants.ACCENT_THEME_COLOR;
                }
            }         
        }

        private void RbUnchecked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = e.OriginalSource as RadioButton;

            for (int i = 0; i < _tbs.Length; i++)
            {
                if (rb.Content.ToString() == _tbs[i].Text)
                {
                    _tbs[i].Foreground = Devices.Helpers.Constants.BODY_THEME_COLOR;
                    DependencyObject parent = LogicalTreeHelper.GetParent(_tbs[i]);
                    ((Control)parent).Background = Devices.Helpers.Constants.LIGHT_THEME_COLOR;
                }
            }
        }
    }
}
