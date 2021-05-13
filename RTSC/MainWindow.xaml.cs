using System.Windows;
using System.Windows.Input;

namespace RTSC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {         
            InitializeComponent();
            KeyDown += ShowHideDebugSettingsButton;
        }

        private void MainMenuItemSelect(object sender, MouseButtonEventArgs e)
        {
            MenuToggleButton.IsChecked = false;
        }

        private void ShowHideDebugSettingsButton(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.L)
            {
                debugSettingsButton.Visibility = debugSettingsButton.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
            }
        }
    }
}
