using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2007.ViewModel;

namespace RTSC.DialogWindows
{
    class TA2007ChannelSelectionViewModel : ViewModelBase
    {
        private List<TA2007ChannelsListItem> _channelsList;
        private TA2007TestMode _testMode;

        public TA2007ChannelSelectionViewModel()
        {
            _channelsList = new List<TA2007ChannelsListItem>();

            for (int i = 1; i <= 32; i++)
            {
                _channelsList.Add(new TA2007ChannelsListItem(i, false));
            }
        }

        public TA2007TestMode TestMode 
        {
            get => _testMode;
            set
            {
                if (value == TA2007TestMode.ErrorCalculation)
                {
                    _channelsList[0].IsChecked = true;
                    _channelsList[0].IsEnabled = false;
                }
                else
                {
                    _channelsList[0].IsEnabled = true;
                }

                Set(ref _testMode, value);
            }
        }
        public ICollectionView Channels => CollectionViewSource.GetDefaultView(
                new ObservableCollection<TA2007ChannelsListItem>(_channelsList)
            );
        
        public RelayCommand ApplyChannelsCommand => new RelayCommand(() =>
        {
            IEnumerable<int> selectedChannels = _channelsList.Where(d => d.IsChecked).Select(d => d.Number);
            Messenger.Default.Send<TA2007ChannelsSettings>(new TA2007ChannelsSettings() { SelectedChannales = selectedChannels });

            DialogHost.CloseDialogCommand.Execute(null, null); 
        });
        public RelayCommand ResetChannelsCommand => new RelayCommand(() =>
        {
            for (int i = 0; i < _channelsList.Count; i++)
            {
                if (TestMode == TA2007TestMode.ErrorCalculation && i == 0)
                {
                    continue;
                }
                _channelsList[i].IsChecked = false;
            }
        });
    }

    class TA2007ChannelsListItem : INotifyPropertyChanged, ICloneable
    {
        private bool _isChecked;
        private bool _isEnabled;

        public TA2007ChannelsListItem(int number, bool isChecked)
        {
            Number = number;
            IsChecked = isChecked;
            IsEnabled = true;
        }

        public int Number { get; set; }
        public bool IsChecked
        {
            get => _isChecked;
            set => this.MutateVerbose(ref _isChecked, value, args => PropertyChanged?.Invoke(this, args));
        }
        public bool IsEnabled
        {
            get => _isEnabled;
            set => this.MutateVerbose(ref _isEnabled, value, args => PropertyChanged?.Invoke(this, args));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public object Clone()
        {
            return new TA2007ChannelsListItem(Number, IsChecked);
        }
    }
    class TA2007ChannelsSettings
    {
        public IEnumerable<int> SelectedChannales { get; set; }
    }
}
