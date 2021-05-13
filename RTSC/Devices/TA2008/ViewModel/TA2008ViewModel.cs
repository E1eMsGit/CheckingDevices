using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2008.Model;
using RTSC.DialogWindows;
using RTSC.ViewModel;

namespace RTSC.Devices.TA2008.ViewModel
{
    class TA2008ViewModel : ViewModelBase
    {
        private TA2008Model _ta2008Model;
        private Log _log;
        private bool _canStart;
        private byte _selectedAddress;
        private byte _selectedChannels;
        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;

        public TA2008ViewModel()
        {
            _log = Log.GetInstance();
            _ta2008Model = new TA2008Model();
            _canStart = true;

            ListAddressesCount = Constants.LIST_ADDRESSES_COUNT;
            ListChannelsCount = new List<byte>();
            for (byte i = 1; i <= 12; i++)
            {
                ListChannelsCount.Add(i);
            }

            Channels = new List<TA2008Channel>()
            {
                new TA2008Channel("1-я восьмерка",  _ta2008Model.Channels[0]) { Margin = new Thickness(0, 0, 5, 5) },
                new TA2008Channel("2-я восьмерка",  _ta2008Model.Channels[1]) { Margin = new Thickness(5, 0, 5, 5) },
                new TA2008Channel("3-я восьмерка",  _ta2008Model.Channels[2]) { Margin = new Thickness(5, 0, 5, 5) },
                new TA2008Channel("4-я восьмерка",  _ta2008Model.Channels[3]) { Margin = new Thickness(5, 0, 5, 5) },
                new TA2008Channel("5-я восьмерка",  _ta2008Model.Channels[4]) { Margin = new Thickness(5, 0, 5, 5) },
                new TA2008Channel("6-я восьмерка",  _ta2008Model.Channels[5]) { Margin = new Thickness(5, 0, 0, 5) },
                
                new TA2008Channel("7-я восьмерка",  _ta2008Model.Channels[6]) { Margin = new Thickness(0, 5, 5, 0) },
                new TA2008Channel("8-я восьмерка",  _ta2008Model.Channels[7]) { Margin = new Thickness(5, 5, 5, 0) },
                new TA2008Channel("9-я восьмерка",  _ta2008Model.Channels[8]) { Margin = new Thickness(5, 5, 5, 0) },
                new TA2008Channel("10-я восьмерка",  _ta2008Model.Channels[9]) { Margin = new Thickness(5, 5, 5, 0) },
                new TA2008Channel("11-я восьмерка",  _ta2008Model.Channels[10]) { Margin = new Thickness(5, 5, 5, 0) },
                new TA2008Channel("12-я восьмерка",  _ta2008Model.Channels[11]) { Margin = new Thickness(5, 5, 0, 0) }
            };

            SelectedChannels = Properties.Settings.Default.TA2008Channels;
            SelectedAddress = Properties.Settings.Default.TA2008Address;         
            ComboBoxEnabled = true;
            ProgressBarVisibility = Visibility.Hidden;
        }

        public List<TA2008Channel> Channels { get; }
        public Visibility ProgressBarVisibility { get; set; }
        public bool ComboBoxEnabled { get; set; }
        public string StartButtonText => _canStart ? "Пуск" : "Стоп";
        public List<byte> ListAddressesCount { get; }
        public byte SelectedAddress
        {
            get => _selectedAddress;
            set 
            {
                if (_selectedAddress == value)
                    return;

                Set(ref _selectedAddress, value);
                Properties.Settings.Default.TA2008Address = value;
                Properties.Settings.Default.Save();

                _ta2008Model.Prepare(value, SelectedChannels);
            }
        }
        public List<byte> ListChannelsCount { get; }
        public byte SelectedChannels
        {
            get => _selectedChannels;
            set
            {
                if (_selectedChannels == value)
                    return;

                Set(ref _selectedChannels, value);

                for (int i = 0; i < Channels.Count; i++)
                {
                    if (i < _selectedChannels)
                    {
                        Channels[i].Visibility = Visibility.Visible;
                    }
                    else
                    {
                        Channels[i].Visibility = Visibility.Hidden;
                    }
                } 

                Properties.Settings.Default.TA2008Channels = value;
                Properties.Settings.Default.Save();
            }
        }

        public RelayCommand StartCommand => new RelayCommand(
            async () =>
            {
                if (_canStart)
                {
                    _cancelTokenSource = new CancellationTokenSource();
                    _token = _cancelTokenSource.Token;

                    _canStart = false;
                    RaisePropertyChanged(nameof(StartButtonText));
                    ProgressBarVisibility = Visibility.Visible;
                    RaisePropertyChanged(nameof(ProgressBarVisibility));
                    ComboBoxEnabled = false;
                    RaisePropertyChanged(nameof(ComboBoxEnabled));

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = false });

                    _log.Write("Проверка ТА2008 начата");

                    _ta2008Model.Prepare(SelectedAddress, SelectedChannels);

                    await _ta2008Model.StartAsync(_token);

                    _canStart = true;
                    RaisePropertyChanged(nameof(StartButtonText));
                    ProgressBarVisibility = Visibility.Hidden;
                    RaisePropertyChanged(nameof(ProgressBarVisibility));
                    ComboBoxEnabled = true;
                    RaisePropertyChanged(nameof(ComboBoxEnabled));

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = true });

                    _log.Write("Проверка ТА2008 завершена\n");
                }
                else
                {
                    _cancelTokenSource.Cancel();
                }               
            });
    }

    class TA2008Channel : INotifyPropertyChanged
    {
        private Visibility _visibility;
        private List<TA2008ChannelContent> _collection;
        public TA2008Channel(string title, List<TA2008ChannelContent> collection)
        {
            Title = title;
            _collection = collection;
        }

        public ICollectionView SourceItemsChannel => CollectionViewSource.GetDefaultView(
                new ObservableCollection<TA2008ChannelContent>(_collection)
            );
        public string Title { get; set; }
        public Thickness Margin { get; set; }
        public Visibility Visibility
        {
            get => _visibility;
            set => this.MutateVerbose(ref _visibility, value, args => PropertyChanged?.Invoke(this, args));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
