using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using RTSC.Devices.Helpers;
using GalaSoft.MvvmLight.Messaging;
using System;
using FTD2XX_NET;
using System.Collections.ObjectModel;
using MaterialDesignThemes.Wpf;
using RTSC.Devices.TA2008.View;
using RTSC.Devices.Debug.View;
using RTSC.Devices.TA1004M1.View;
using RTSC.Devices.TA2006.View;
using RTSC.Devices.TA2007.View;
using RTSC.DialogWindows;

namespace RTSC.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        private Log _log;
        private FtdiDevice[] _devices;
        private uint _numDevices;
        private bool _mainPanelEnabled;
        private ObservableCollection<MainMenuItem> _mainMenuItems;
        private MainMenuItem _selectedItem;
        private bool _isHeaderEnabled;
        private int _defaultMenuIndex;

        public MainWindowViewModel()
        {
            Messenger.Default.Register<HeaderEnable>(this, (o) => IsHeaderEnabled = o.IsEnabled);

            _log = Log.GetInstance();
            _log.Write($"Отчет {DateTime.Now.ToShortDateString()}\n", false);

            new FTDI().GetNumberOfDevices(ref _numDevices);
#if DEBUG
            Console.WriteLine($"NUMBER OF DEVICES ---- {_numDevices}");
#endif
            if (_numDevices == 2)
            {
                _devices = new FtdiDevice[_numDevices];

                for (uint i = 0; i < _devices.Length; i++)
                {
                    _devices[i] = new FtdiDevice(i);
                    _devices[i].Open();
                }               
            }         

            MainMenuItems = new ObservableCollection<MainMenuItem>(GenerateMainMenuItems());
            DefaultMenuIndex = Properties.Settings.Default.DefaultMenuIndex;
        }

        public bool MainPanelEnabled
        {
            get => _mainPanelEnabled;
            set => Set(ref _mainPanelEnabled, value);
        }
        public bool IsHeaderEnabled 
        {
            get => _isHeaderEnabled;
            set => Set(ref _isHeaderEnabled, value);
        }
        public ObservableCollection<MainMenuItem> MainMenuItems
        {
            get => _mainMenuItems;
            set =>  Set(ref _mainMenuItems, value);
        }
        public MainMenuItem SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value == null || value.Equals(_selectedItem)) return;
                Set(ref _selectedItem, value); 
            }
        }
        public int DefaultMenuIndex {
            get => _defaultMenuIndex;
            set 
            {
                _defaultMenuIndex = value;
                Properties.Settings.Default.DefaultMenuIndex = _defaultMenuIndex;
                Properties.Settings.Default.Save();
            } 
        }

        public RelayCommand LoadedWindowCommand => new RelayCommand(
            () =>
            {
                Messenger.Default.Send(_devices);
                Messenger.Default.Send(new TK158Settings() { 
                    Frequency = (Frequency)Properties.Settings.Default.Frequency,
                    BitsCount = Properties.Settings.Default.BitsCount,
                    IsInfiniteFT = Properties.Settings.Default.IsInfiniteFT,
                    WordLength = (WordLength)Properties.Settings.Default.WordLength
                });

                CheckIsDeviceOpen();
            });
        public RelayCommand CloseWindowCommand => new RelayCommand(
            () =>
            {
                if (_devices != null)
                {
                    for (uint i = 0; i < _devices.Length; i++)
                    {
                        _devices[i].Close();
                    }
                }

                Application.Current.Shutdown();
            });
        public RelayCommand SaveLogCommand => new RelayCommand(() => _log.SaveLog());
        public RelayCommand OpenHelpCommand => new RelayCommand(() => System.Diagnostics.Process.Start("help.html"));
        public RelayCommand TK158SettingsCommand => new RelayCommand(() => Dialog.ShowTK158SettingsDialog(_devices));
        public RelayCommand DebugSettingsCommand => new RelayCommand(() => Dialog.ShowDebugSettingsDialog());

        private void CheckIsDeviceOpen()
        {
            if (_numDevices == 2 && _devices[0].IsDeviceOpen == true && _devices[1].IsDeviceOpen == true)
            {
                MainPanelEnabled = true;
                IsHeaderEnabled = true;
            }
            else
            {
#if DEBUG
                // Для отладки без устройства.
                MainPanelEnabled = true;
                IsHeaderEnabled = true;
#else
                Dialog.ShowNotificationDialog("Ошибка", "Подключите ТК158 и перезапустите программу");
                MainPanelEnabled = false;
                IsHeaderEnabled = false;
#endif
            }
        }
        private ObservableCollection<MainMenuItem> GenerateMainMenuItems() => 
            new ObservableCollection<MainMenuItem>
            {
                new MainMenuItem("TA 1004M1", new TA1004M1View(), PackIconKind.Vibration),
                new MainMenuItem("ТА 2006", new TA2006View(), PackIconKind.Analog),
                new MainMenuItem("ТА 2007", new TA2007View(), PackIconKind.TemperatureCelsius),
                new MainMenuItem("ТА 2008", new TA2008View(), PackIconKind.Numeric10),           
                new MainMenuItem("Отладка", new DebugView(), PackIconKind.PackageVariant) { Margin = new Thickness(0, 30, 0, 0) },              
            };
    }

    class MainMenuItem
    {
        public MainMenuItem(string name, object content, PackIconKind icon)
        {
            Name = name;
            Content = content;
            Icon = icon;
            Margin = new Thickness(0);
        }

        public string Name { get; set; }
        public object Content { get; set; }
        public PackIconKind Icon { get; set; }
        public Thickness Margin { get; set; }
    }
    class HeaderEnable
    {
        public bool IsEnabled { get; set; }
    }
}
