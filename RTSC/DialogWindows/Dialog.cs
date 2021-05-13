using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2007.ViewModel;

namespace RTSC.DialogWindows
{
    static class Dialog
    {
        public static async void ShowNotificationDialog(string title, string message)
        {
            var view = new NotificationDialogView
            {
                DataContext = new NotificationDialogViewModel() { Title = title, Message = message }
            };

            // Костыль для того чтобы Dialoghost успел загрузиться в LoadedWindowCommand.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            await DialogHost.Show(view, "RootDialog");
        }

        public static async void ShowChannelSelectionDialog()
        {
            // Костыль для того чтобы Dialoghost успел загрузиться в LoadedWindowCommand.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            await DialogHost.Show(new TA2007ChannelSelectionView(), "RootDialog");
        }

        public static async void ShowTK158SettingsDialog(FtdiDevice[] devices)
        {
            var view = new TK158SettingsView
            {
                DataContext = new TK158SettingsViewModel() { Devices = devices }
            };

            // Костыль для того чтобы Dialoghost успел загрузиться в LoadedWindowCommand.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            await DialogHost.Show(view, "RootDialog");
        }

        public static async void ShowDebugSettingsDialog()
        {
            var view = new DebugSettingsDialogView
            {
                DataContext = new DebugSettingsDialogViewModel()
            };

            // Костыль для того чтобы Dialoghost успел загрузиться в LoadedWindowCommand.
            await Task.Delay(TimeSpan.FromMilliseconds(1));

            await DialogHost.Show(view, "RootDialog");
        }
    }
}
