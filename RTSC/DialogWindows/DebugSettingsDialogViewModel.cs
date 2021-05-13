using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;

namespace RTSC.DialogWindows
{
    class DebugSettingsDialogViewModel : ViewModelBase
    {
        private byte _ta2006SelectedDelay;
        private byte _ta2007SelectedDelay;
        private byte _ta2008SelectedDelay;
        private byte _ta1004M1SelectedDelay;

        public DebugSettingsDialogViewModel()
        {        
            TA1004M1DelayCount = new List<byte>();
            TA2006DelayCount = new List<byte>();
            TA2007DelayCount = new List<byte>();          
            TA2008DelayCount = new List<byte>();

            for (byte i = 0; i <= 12; i++)
            {
                TA1004M1DelayCount.Add(i);
                TA2006DelayCount.Add(i);
                TA2007DelayCount.Add(i);             
                TA2008DelayCount.Add(i);
            }

            TA1004M1SelectedDelay = Properties.Settings.Default.TA1004M1Delay;
            TA2006SelectedDelay = Properties.Settings.Default.TA2006Delay;
            TA2007SelectedDelay = Properties.Settings.Default.TA2007Delay;
            TA2008SelectedDelay = Properties.Settings.Default.TA2008Delay;
            TA2007TimerValue = Properties.Settings.Default.TA2007TimerValue;
        }

        public List<byte> TA1004M1DelayCount { get; }
        public List<byte> TA2006DelayCount { get; }
        public List<byte> TA2007DelayCount { get; }
        public List<byte> TA2008DelayCount { get; }
        public byte TA1004M1SelectedDelay
        {
            get => _ta1004M1SelectedDelay;
            set
            {
                if (_ta1004M1SelectedDelay == value)
                    return;

                Set(ref _ta1004M1SelectedDelay, value);
            }
        }
        public byte TA2006SelectedDelay
        {
            get => _ta2006SelectedDelay;
            set
            {
                if (_ta2006SelectedDelay == value)
                    return;

                Set(ref _ta2006SelectedDelay, value);
            }
        }
        public byte TA2007SelectedDelay
        {
            get => _ta2007SelectedDelay;
            set
            {
                if (_ta2007SelectedDelay == value)
                    return;

                Set(ref _ta2007SelectedDelay, value);
            }
        }
        public byte TA2008SelectedDelay
        {
            get => _ta2008SelectedDelay;
            set
            {
                if (_ta2008SelectedDelay == value)
                    return;

                Set(ref _ta2008SelectedDelay, value);
            }
        }
        public int TA2007TimerValue { get; set; }
        
        public RelayCommand ApplyDebugSettingsCommand => new RelayCommand(
            () =>
            {
                Messenger.Default.Send<DebugSettings>(new DebugSettings() {                                       
                    TA1004M1Delay = TA1004M1SelectedDelay, 
                    TA2006Delay = TA2006SelectedDelay,
                    TA2007Delay = TA2007SelectedDelay,
                    TA2008Delay = TA2008SelectedDelay,
                    TA2007TimerValue = this.TA2007TimerValue
                });

                
                Properties.Settings.Default.TA1004M1Delay = TA1004M1SelectedDelay;
                Properties.Settings.Default.TA2006Delay = TA2006SelectedDelay;               
                Properties.Settings.Default.TA2007Delay = TA2007SelectedDelay;               
                Properties.Settings.Default.TA2008Delay = TA2008SelectedDelay;                        
                Properties.Settings.Default.TA2007TimerValue = TA2007TimerValue;                        
                Properties.Settings.Default.Save();

                DialogHost.CloseDialogCommand.Execute(null, null);
            });
    }

    class DebugSettings
    {
        public byte TA1004M1Delay { get; set; }
        public byte TA2006Delay { get; set; }
        public byte TA2007Delay { get; set; }
        public byte TA2008Delay { get; set; }
        public int TA2007TimerValue { get; set; }
    }
}
