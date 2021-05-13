using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2007.Model;
using RTSC.DialogWindows;
using RTSC.ViewModel;

namespace RTSC.Devices.TA2007.ViewModel
{
    class TA2007ViewModel : ViewModelBase
    {
        private TA2007Model _ta2007Model;

        private Log _log;
        private bool _canStart;
        private byte _selectedAddress;
        private List<int> _selectedChannelsList;
        private string _testTitle;
        private bool _isPhasing;
        private string _lastResult;

        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;

        private bool _isFunctionCheckTestChecked; // Проверка функционирования.
        private bool _isErrorCalculationTestChecked; // Расчет погрешности.
        private bool _isResistanceTestChecked; // Сопротивление.
        private bool _isVoltageTestChecked; // Напряжение.

        private string _valueHintText;
        private double _valueForSecondTest; // Значение для проверки напряжения или сопротивления.

        private int _timerValue;
        private DispatcherTimer _timer;
        private float _timerStep;

        public TA2007ViewModel()
        {
            Messenger.Default.Register<TA2007ChannelsSettings>(this, (o) =>
            {
                _selectedChannelsList = o.SelectedChannales.ToList();
                ResetNumberColor();
                PaintSelectedChannels();
                RaisePropertyChanged(nameof(IsFunctionCheckTestChecked));
                RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));         
            });
            Messenger.Default.Register<Brush[]>(this, (o) => 
            { 
                PhasingColor = o[0];
                SyncColor = o[1];
                RaisePropertyChanged(nameof(PhasingColor));
                RaisePropertyChanged(nameof(SyncColor));
            });
            Messenger.Default.Register<TA2007ResultText>(this, (o) =>
            {
                _lastResult = o.Text;
                ResultText.AppendLine(_lastResult);
                RaisePropertyChanged(nameof(ResultText));
            });
            Messenger.Default.Register<DebugSettings>(this, (o) =>
            {
                _timerValue = o.TA2007TimerValue;
                _timerStep = (float)(100.0 / _timerValue);
            });

            _ta2007Model = new TA2007Model();
            _log = Log.GetInstance();
            _canStart = true;
            _selectedChannelsList = new List<int>();
            IsFunctionCheckTestChecked = true;
            IsResistanceTestChecked = true;
            IsPhasing = false;
            ResultText = new StringBuilder();

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timerValue = Properties.Settings.Default.TA2007TimerValue;
            _timerStep = (float)(100.0 / _timerValue);

            ListAddressesCount = Constants.LIST_ADDRESSES_COUNT;

            ProgressBarVisibility = Visibility.Hidden;
            IsControlsEnabled = true;
            SelectedAddress = Properties.Settings.Default.TA2007Address;
            IsPhasing = false;          
        }
       
        public StringBuilder ResultText { get; set; }

        public string FunctionCheckTestTitle => "Проверка функционирования";
        public string ErrorCalculationTestTitle => "Расчет прогрешности";
        public string ResistanceTestTitle => "Сопротивление";
        public string VoltageTestTitle => "Напряжение";
        public string StartButtonText => _canStart ? "Пуск" : "Стоп";

        public string ValueHintText
        {
            get => _valueHintText;
            set => Set(ref _valueHintText, value);            
        }
        public double ValueForSecondTest
        {
            get => _valueForSecondTest;
            set
            {
                Set(ref _valueForSecondTest, value);
                RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));
            }
        }

        public bool IsFunctionCheckTestChecked
        {
            get 
            {
                if (_isFunctionCheckTestChecked)
                {
                    _testTitle = FunctionCheckTestTitle;

                    IsPhasingEnabled = true;
                    RaisePropertyChanged(nameof(IsPhasingEnabled));
                    IsStartButtonEnabled = true;
                    RaisePropertyChanged(nameof(IsStartButtonEnabled));                   
                }
                
                return _isFunctionCheckTestChecked; 
            }
            set => Set(ref _isFunctionCheckTestChecked, value);
        }
        public bool IsErrorCalculationTestChecked
        {
            get
            {
                if (_isErrorCalculationTestChecked)
                {
                    IsPhasingEnabled = false;
                    RaisePropertyChanged(nameof(IsPhasingEnabled));

                    if (_selectedChannelsList.Count == 2)
                    {                     
                        IsStartButtonEnabled = true;
                        RaisePropertyChanged(nameof(IsStartButtonEnabled));
                    }
                    else
                    {
                        IsStartButtonEnabled = false;
                        RaisePropertyChanged(nameof(IsStartButtonEnabled));
                    }
                }
            
                return _isErrorCalculationTestChecked;
            }
            set
            {
                Set(ref _isErrorCalculationTestChecked, value);
            }
           
        }
        public bool IsResistanceTestChecked 
        {
            get
            {
                if (_isResistanceTestChecked)
                {
                    _testTitle = $"{ErrorCalculationTestTitle}: {ResistanceTestTitle}";
                }
                return _isResistanceTestChecked;
            }
            set
            {             
                Set(ref _isResistanceTestChecked, value);
               
                if (_isResistanceTestChecked)
                {                    
                    ValueHintText = "Значение (Ом)";                  
                    RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));
                }                               
            }
        }
        public bool IsVoltageTestChecked
        {
            get
            {
                if (_isVoltageTestChecked)
                {
                    _testTitle = $"{ErrorCalculationTestTitle}: {VoltageTestTitle}";
                }
                return _isVoltageTestChecked;
            }
            set
            {
                Set(ref _isVoltageTestChecked, value);
               
                if (_isVoltageTestChecked)
                {
                    
                    ValueHintText = "Значение (мВ)";
                    RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));
                }         
            }
        }

        public Visibility ProgressBarVisibility { get; set; }
        public float ProgressBarValue { get; set; }
        public bool ProgressBarIndeterminate { get; set; }

        public ICollectionView SourceItemsChannelContentFirstHalf => CollectionViewSource.GetDefaultView(
            new ObservableCollection<TA2007ChannelContent>(_ta2007Model.ChannelContent.Where(d => d.Number <= 16))
            );
        public ICollectionView SourceItemsChannelContentSecondHalf => CollectionViewSource.GetDefaultView(
            new ObservableCollection<TA2007ChannelContent>(_ta2007Model.ChannelContent.Where(d => d.Number > 16))
            );    
        public bool IsPhasing 
        { 
            get
            {
                return _isPhasing;
            }
            set
            {
                Set(ref _isPhasing, value);
                if (_isFunctionCheckTestChecked)
                {
                    RaisePropertyChanged(nameof(IsFunctionCheckTestChecked));
                }
                else
                {
                    RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));
                }
            }
        }
        public bool IsPhasingEnabled { get; set; }
        public List<byte> ListAddressesCount { get; }
        public byte SelectedAddress
        {
            get => _selectedAddress;
            set
            {
                if (_selectedAddress == value)
                    return;

                Set(ref _selectedAddress, value);

                if (_isFunctionCheckTestChecked)
                {
                    RaisePropertyChanged(nameof(IsFunctionCheckTestChecked));
                }
                else
                {
                    RaisePropertyChanged(nameof(IsErrorCalculationTestChecked));
                }

                Properties.Settings.Default.TA2007Address = value;
                Properties.Settings.Default.Save();
            }
        }

        public bool IsChannelsButtonEnabled { get; set; }
        public bool IsControlsEnabled { get; set; }
        public bool IsStartButtonEnabled { get; set; }

        public Brush PhasingColor { get; set; }
        public Brush SyncColor { get; set; }

        public RelayCommand ShowChannelSelectionCommand => new RelayCommand(() =>
        {
            new ViewModelLocator().TA2007ChannelSelection.TestMode = IsFunctionCheckTestChecked? TA2007TestMode.FunctionCheck : TA2007TestMode.ErrorCalculation;
            Dialog.ShowChannelSelectionDialog(); 
        });
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
                    IsChannelsButtonEnabled = false;
                    RaisePropertyChanged(nameof(IsChannelsButtonEnabled));
                    Messenger.Default.Send(new HeaderEnable { IsEnabled = false });
                    _log.Write($"Проверка ТА2007 начата. Режим: {_testTitle}");

                    if (IsFunctionCheckTestChecked)
                    {
                        if (_selectedChannelsList.Count > 0)
                        {
                            _ta2007Model.FunctionCheckPrepare(SelectedAddress, IsPhasing, _selectedChannelsList);
                        }
                        else
                        {
                            _ta2007Model.FunctionCheckPrepare(SelectedAddress, IsPhasing);
                        }
                    }
                    else
                    {
                        if (IsResistanceTestChecked)
                        {
                            _ta2007Model.ErrorCalculationPrepare(SelectedAddress, _selectedChannelsList, _valueForSecondTest, TA2007TestMode.ErrorCalculationResistance);
                        }
                        else if (IsVoltageTestChecked)
                        {
                            _ta2007Model.ErrorCalculationPrepare(SelectedAddress, _selectedChannelsList, _valueForSecondTest, TA2007TestMode.ErrorCalculationVoltage);
                        }                                           
                    }

                    if (_timerValue > 0)
                    {
                        IsStartButtonEnabled = false;
                        ProgressBarIndeterminate = false;
                        RaisePropertyChanged(nameof(IsStartButtonEnabled));
                        RaisePropertyChanged(nameof(ProgressBarIndeterminate));
                        _timer.Start();
                    }
                    else
                    {
                        ProgressBarValue = 0;
                        ProgressBarIndeterminate = true;
                        RaisePropertyChanged(nameof(ProgressBarValue));
                        RaisePropertyChanged(nameof(ProgressBarIndeterminate));
                    }

                    await _ta2007Model.StartAsync(_token);

                    // Если время проверки больше 0.
                    if (!IsStartButtonEnabled)
                    {
                        IsStartButtonEnabled = true;
                        RaisePropertyChanged(nameof(IsStartButtonEnabled));
                    }

                    _canStart = true;
                    RaisePropertyChanged(nameof(StartButtonText));
                    ProgressBarVisibility = Visibility.Hidden;
                    RaisePropertyChanged(nameof(ProgressBarVisibility));
                    IsControlsEnabled = true;
                    RaisePropertyChanged(nameof(IsControlsEnabled));

                    if (IsErrorCalculationTestChecked)
                    {
                        IsChannelsButtonEnabled = true;
                        RaisePropertyChanged(nameof(IsChannelsButtonEnabled));
                    }

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = true });
                    _log.Write($"Проверка ТА2007 завершена. Режим: {_testTitle}");
                    _log.Write(_lastResult);
                }
                else
                {
                    _cancelTokenSource.Cancel();
                }
            });
       
        private void ResetNumberColor()
        {
            for (int i = 0; i < _ta2007Model.ChannelContent.Count; i++)
            {
                _ta2007Model.ChannelContent[i].NumberCellColor = Constants.LIGHT_THEME_COLOR;
                _ta2007Model.ChannelContent[i].NumberFontColor = Constants.BODY_THEME_COLOR;
            }
        }
        private void PaintSelectedChannels()
        {
            if (_selectedChannelsList != null)
            {
                for (int i = 0; i < _selectedChannelsList.Count; i++)
                {
                    _ta2007Model.ChannelContent[_selectedChannelsList[i] - 1].NumberCellColor = Constants.GREEN_COLOR;
                    _ta2007Model.ChannelContent[_selectedChannelsList[i] - 1].NumberFontColor = Constants.WHITE_COLOR;                    
                }
            }
        }
        private void timer_Tick(object sender, EventArgs e)
        {
            ProgressBarValue += _timerStep;
            RaisePropertyChanged(nameof(ProgressBarValue));

            if (ProgressBarValue >= 100)
            {
                _timer.Stop();

                _cancelTokenSource.Cancel();

                ProgressBarValue = 0;
                ProgressBarIndeterminate = true;
                RaisePropertyChanged(nameof(ProgressBarValue));
                RaisePropertyChanged(nameof(ProgressBarIndeterminate));
            }
        }
    }   

    /// <summary>
    /// Режимы проверки прибора.
    /// </summary>
    public enum TA2007TestMode
    {
        /// <summary>
        /// Проверка функционирования.
        /// </summary>
        FunctionCheck = 0,
        /// <summary>
        /// Расчет погрешности.
        /// </summary>
        ErrorCalculation = 1,
        /// <summary>
        /// Расчет погрешности: Сопротивление.
        /// </summary>
        ErrorCalculationResistance = 2,
        /// <summary>
        /// Расчет погрешности: Напряжение.
        /// </summary>
        ErrorCalculationVoltage = 3,
    }
}
