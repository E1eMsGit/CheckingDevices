using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2006.Model;
using RTSC.DialogWindows;
using RTSC.ViewModel;

namespace RTSC.Devices.TA2006.ViewModel
{
    class TA2006ViewModel : ViewModelBase
    {
        private TA2006Model _ta2006Model;
        private Log _log;
        private byte _selectedAddress;
        private bool _canStart;
        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;
        private TA2006CommutatorsNum _commNum;

        public TA2006ViewModel()
        {
            _ta2006Model = new TA2006Model();
            _log = Log.GetInstance();
            _canStart = true;

            IsControlsEnabled = true;
            ListAddressesCount = Constants.LIST_ADDRESSES_COUNT;
            ProgressBarVisibility = Visibility.Hidden;
            SelectedAddress = Properties.Settings.Default.TA2006Address;
            CommNum = TA2006CommutatorsNum.First;

            _ta2006Model.PropertyChanged += ChangeCommutator_PropertyChanged;
        }
        
        public TA2006CommutatorsNum CommNum
        {
            get
            {
                _ta2006Model.ChangeCommutator((byte)_commNum);
                return _commNum;
            }
            set
            {
                if (_commNum == value)
                    return;

                Set(ref _commNum, value);
                RaisePropertyChanged(nameof(IsFirstComm));
                RaisePropertyChanged(nameof(IsSecondComm));
                RaisePropertyChanged(nameof(IsThirdComm));               
            }
        }
        public bool IsFirstComm
        {
            get => CommNum == TA2006CommutatorsNum.First;
            set => Set(ref _commNum, value ? TA2006CommutatorsNum.First : CommNum);
        }
        public bool IsSecondComm 
        {
            get => CommNum == TA2006CommutatorsNum.Second;
            set => Set(ref _commNum, value ? TA2006CommutatorsNum.Second : CommNum);
        }
        public bool IsThirdComm 
        {
            get => CommNum == TA2006CommutatorsNum.Third;
            set => Set(ref _commNum, value ? TA2006CommutatorsNum.Third : CommNum);
        }
        public List<byte> ListAddressesCount { get; }
        public byte SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                if (_selectedAddress == value)
                    return;

                Set(ref _selectedAddress, value);

                Properties.Settings.Default.TA2006Address = value;
                Properties.Settings.Default.Save();
            }
        }
        public bool IsControlsEnabled { get; set; }
        public Visibility ProgressBarVisibility { get; set; }
        public string StartButtonText => _canStart ? "Пуск" : "Стоп";
        public ICollectionView SourceItemsChannelContentFirstHalf => CollectionViewSource.GetDefaultView(
            new ObservableCollection<TA2006ChannelContent>(_ta2006Model.ChannelContent.Where(
                d => d.Number <= _ta2006Model.ChannelContent[_ta2006Model.ChannelContent.Count / 2 - 1].Number))
            );
        public ICollectionView SourceItemsChannelContentSecondHalf => CollectionViewSource.GetDefaultView(
            new ObservableCollection<TA2006ChannelContent>(_ta2006Model.ChannelContent.Where(
                d => d.Number > _ta2006Model.ChannelContent[_ta2006Model.ChannelContent.Count / 2 - 1].Number))
            );


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
                   IsControlsEnabled = false;
                   RaisePropertyChanged(nameof(IsControlsEnabled));
                   Messenger.Default.Send(new HeaderEnable { IsEnabled = false });
                   _log.Write("Проверка ТА2006 начата.");

                   _ta2006Model.Prepare(SelectedAddress);
                   await _ta2006Model.StartAsync(_token);

                   _canStart = true;
                   RaisePropertyChanged(nameof(StartButtonText));
                   ProgressBarVisibility = Visibility.Hidden;
                   RaisePropertyChanged(nameof(ProgressBarVisibility));
                   IsControlsEnabled = true;
                   RaisePropertyChanged(nameof(IsControlsEnabled));

                   Messenger.Default.Send(new HeaderEnable { IsEnabled = true });
                   _log.Write("Проверка ТА2006 завершена.\n");
               }
               else
               {
                   _cancelTokenSource.Cancel();
               }
           });

        private void ChangeCommutator_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChannelContent")
            {
                RaisePropertyChanged("SourceItemsChannelContentFirstHalf");
                RaisePropertyChanged("SourceItemsChannelContentSecondHalf");
            }                
        }
    }

    enum TA2006CommutatorsNum
    {
        First = 1,
        Second = 2,
        Third = 3
    }
}
