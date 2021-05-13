using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using RTSC.Devices.Helpers;

namespace RTSC.DialogWindows
{
    class TK158SettingsViewModel : ViewModelBase
    {
        private FtdiDevice[] _devices;
        private byte _selectedBits;
        private bool _isInfiniteFT;
        private Frequency _frequency;
        private WordLength _wordLength;

        public TK158SettingsViewModel()
        {
            SelectedBitsCount = Properties.Settings.Default.BitsCount;
            Frequency = (Frequency)Properties.Settings.Default.Frequency;
            WordLength = (WordLength)Properties.Settings.Default.WordLength;
            IsInfiniteFT = Properties.Settings.Default.IsInfiniteFT;

            ListBitsCount = new List<byte>();
            for (byte i = 1; i <= 32; i++)
            {
                ListBitsCount.Add(i);
            }                      
        }

        public FtdiDevice[] Devices { 
            get => _devices;
            set => _devices = value;
        }

        public Frequency Frequency
        {
            get => _frequency; 
            set
            {
                if (_frequency == value)
                    return;

                Set(ref _frequency, value);
                RaisePropertyChanged(nameof(Is128kHz));
                RaisePropertyChanged(nameof(Is256kHz));
                RaisePropertyChanged(nameof(Is512kHz));
                RaisePropertyChanged(nameof(Is1024kHz));
            }
        }
        public bool Is128kHz 
        { 
            get => Frequency == Frequency._128kHz;
            set => Set(ref _frequency, value ? Frequency._128kHz : Frequency);
        }
        public bool Is256kHz
        {
            get => Frequency == Frequency._256kHz;
            set => Set(ref _frequency, value ? Frequency._256kHz : Frequency);
        }
        public bool Is512kHz
        {
            get => Frequency == Frequency._512kHz;
            set => Set(ref _frequency, value ? Frequency._512kHz : Frequency);
        }
        public bool Is1024kHz
        {
            get => Frequency == Frequency._1024kHz;
            set => Set(ref _frequency, value ? Frequency._1024kHz : Frequency);
        }

        public WordLength WordLength
        {
            get => _wordLength;
            set
            {
                if (_wordLength == value)
                    return;

                Set(ref _wordLength, value);
                RaisePropertyChanged(nameof(Is16Bit));
                RaisePropertyChanged(nameof(Is32Bit));
            }
        }
        public bool Is16Bit 
        {
            get => WordLength == WordLength._16Bit;
            set => Set(ref _wordLength, value ? WordLength._16Bit : WordLength);
        }
        public bool Is32Bit
        {
            get => WordLength == WordLength._32Bit;
            set => Set(ref _wordLength, value ? WordLength._32Bit : WordLength);
        }

        public bool IsInfiniteFT 
        {
            get => _isInfiniteFT;
            set => Set(ref _isInfiniteFT, value);
        }

        public List<byte> ListBitsCount { get; }
        public byte SelectedBitsCount { 
            get => _selectedBits; 
            set => Set(ref _selectedBits, value); 
        }

        public RelayCommand SendTK158SettingsCommand => new RelayCommand(
            async () =>
            {             
                if (_devices != null && _devices[0].IsDeviceOpen == true && _devices[1].IsDeviceOpen == true)
                {                    
                    Messenger.Default.Send(new TK158Settings() { 
                        Frequency = this.Frequency,
                        BitsCount = this.SelectedBitsCount, 
                        IsInfiniteFT = this.IsInfiniteFT, 
                        WordLength = this.WordLength
                    });

                    Properties.Settings.Default.Frequency = (byte)Frequency;
                    Properties.Settings.Default.BitsCount = SelectedBitsCount;
                    Properties.Settings.Default.IsInfiniteFT = IsInfiniteFT;
                    Properties.Settings.Default.WordLength = (byte)WordLength;
                    Properties.Settings.Default.Save();

                    var wordLengthWithMod = Is16Bit ? SelectedBitsCount : (byte)(SelectedBitsCount | 0x80);
                    wordLengthWithMod = IsInfiniteFT ? (byte)(wordLengthWithMod | 0x40) : wordLengthWithMod;

                    await _devices[0].WriteByteAsync((byte)Frequency)
                    .ContinueWith(t => _devices[1].WriteByteAsync(wordLengthWithMod));
                    await _devices[0].ReadBytesAsync().ContinueWith(t => _devices[1].ReadBytesAsync());

                    if ((byte)WordLength == (byte)WordLength._32Bit)
                    {
                        await _devices[0].WriteByteAsync(0x0).ContinueWith(t => _devices[1].WriteByteAsync(0x0));
                        await _devices[0].ReadBytesAsync().ContinueWith(t => _devices[1].ReadBytesAsync());
                    }
                }

                DialogHost.CloseDialogCommand.Execute(null, null);
            });
    }

    class TK158Settings
    {
        public Frequency Frequency { get; set; }
        public byte BitsCount { get; set; }
        public bool IsInfiniteFT { get; set; }
        public WordLength WordLength { get; set; }
    }

    public enum Frequency
    {
        _128kHz = 0x3F,
        _256kHz = 0x1F,
        _512kHz = 0x0F,
        _1024kHz = 0x07,
    }

    public enum WordLength
    {
        _16Bit = 0x10,
        _32Bit = 0x20,
    }
}
