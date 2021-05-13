using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using RTSC.Devices.TA1004M1.Model;
using RTSC.Devices.Helpers;
using RTSC.ViewModel;
using System.Collections.Generic;
using System;

namespace RTSC.Devices.TA1004M1.ViewModel
{
    class TA1004M1ViewModel : ViewModelBase
    {
        private Log _log;
        private TA1004M1TestMode _selectedMode;

        private PassingInformationModel _passingInformationModel;
        private OutputInformationModel _outputInformationModel;
        private OtherTestsModel _otherTestsModel;
        private IModel _model;

        private bool _canStart;

        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;

        private string _address1;
        private string _address2;

        public TA1004M1ViewModel()
        {            
            _log = Log.GetInstance();
            _canStart = true;
            _passingInformationModel = new PassingInformationModel();
            _outputInformationModel = new OutputInformationModel();
            _otherTestsModel = new OtherTestsModel();

            Address1 = Properties.Settings.Default.TA1004M1Address1;
            Address2 = Properties.Settings.Default.TA1004M1Address2;
            
            ProgressBarVisibility = Visibility.Hidden;
            IsPanelEnabled = true;
            
            Modes = new List<TA1004M1TestMode>()
            {
                new TA1004M1TestMode("Проверка тока потребления", NumbersOfModes.Mode1),
                new TA1004M1TestMode("Проверка прохождения информации", NumbersOfModes.Mode2),
                new TA1004M1TestMode("Проверка адресатора", NumbersOfModes.Mode3),
                new TA1004M1TestMode("Формирование ЗИ и Тоср", NumbersOfModes.Mode4) { Margin = new Thickness(0, 15, 0, 10) },
                new TA1004M1TestMode("Измерение параметров сигналов Тоср", NumbersOfModes.Mode5),
                new TA1004M1TestMode("Измерение параметров сигналов ЗИ1 и ЗИ2", NumbersOfModes.Mode6),
                new TA1004M1TestMode("Измерение параметров сигналов ЗИ3 и ЗИ4", NumbersOfModes.Mode7),
                new TA1004M1TestMode("Проверка входов информации", NumbersOfModes.Mode8) { Margin = new Thickness(0, 15, 0, 10) },
                new TA1004M1TestMode("Проверка разрядов кода", NumbersOfModes.Mode9),
                new TA1004M1TestMode("Проверка задержки выдачи на АУ1", NumbersOfModes.Mode10),
                new TA1004M1TestMode("Проверка задержки выдачи на АУ2", NumbersOfModes.Mode11),
            };
            SelectedMode = Modes[0];

            _outputInformationModel.PropertyChanged += ModelCodeBits_PropertyChanged;
            _passingInformationModel.PropertyChanged += ModelResult_PropertyChanged;
        }

        public ICollectionView SourceItemsPassingInformation => CollectionViewSource.GetDefaultView(
                new ObservableCollection<PassingInformationContent>(_passingInformationModel.PassingInformation)
            );
        public ICollectionView SourceItemsOutputInformation => CollectionViewSource.GetDefaultView(
                new ObservableCollection<OutputInformationContent>(_outputInformationModel.OutputInformation)
            );

        public List<TA1004M1TestMode> Modes { get; }
        public TA1004M1TestMode SelectedMode
        {
            get => _selectedMode;
            set
            {
                if (_selectedMode == value)
                {
                    return;
                }

                Set(ref _selectedMode, value);
            }
        }

        public byte? CodeBitsResult => _outputInformationModel.CodeBits;
        public string ResultMessage => _passingInformationModel.Result;
        
        public string StartButtonText => _canStart ? "Пуск" : "Стоп";
        public Visibility ProgressBarVisibility { get; set; }
        public bool IsPanelEnabled { get; set; }

        public string Address1
        {
            get => _address1;
            set
            {
                _address1 = value;
                Properties.Settings.Default.TA1004M1Address1 = value;
                Properties.Settings.Default.Save();
            }
        }
        public string Address2
        {
            get => _address2;
            set
            {
                _address2 = value;
                Properties.Settings.Default.TA1004M1Address2 = value;
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
                    IsPanelEnabled = false;
                    RaisePropertyChanged(nameof(IsPanelEnabled));

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = false });
                    
                    switch (SelectedMode.ModeNum)
                    {
                        case NumbersOfModes.Mode1:  // Проверка тока потребления.                            
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode1();
                            break;
                        case NumbersOfModes.Mode2: // Проверка прохождения информации.                          
                            _model = _passingInformationModel;
                            ((PassingInformationModel)_model).Prepare(Address1, Address2);
                            break;
                        case NumbersOfModes.Mode3: // Проверка адресатора.                         
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode2();
                            break;
                        case NumbersOfModes.Mode4: // Формирование ЗИ и Тоср.                         
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode3(Address1, Address2);
                            break;
                        case NumbersOfModes.Mode5: // Тоср.                          
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode4(Address1);
                            break;
                        case NumbersOfModes.Mode6: // ЗИ1 и ЗИ2.                          
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode5(Address1);
                            break;
                        case NumbersOfModes.Mode7: // ЗИ3 и ЗИ4.                           
                            _model = _otherTestsModel;
                            ((OtherTestsModel)_model).PrepareMode6(Address2);
                            break;                       
                        case NumbersOfModes.Mode8: // Входы информации.                            
                            _model = _outputInformationModel;
                            ((OutputInformationModel)_model).PrepareMode1(Address1, Address2);
                            break;
                        case NumbersOfModes.Mode9: // Разряды кода.                            
                            _model = _outputInformationModel;
                            ((OutputInformationModel)_model).PrepareMode2(Address1);
                            break;
                        case NumbersOfModes.Mode10: // Задержка выдачи на АУ1.                           
                            _model = _outputInformationModel;
                            ((OutputInformationModel)_model).PrepareMode3(Address1);
                            break;
                        case NumbersOfModes.Mode11: // Задержка выдачи на АУ2.                          
                            _model = _outputInformationModel;
                            ((OutputInformationModel)_model).PrepareMode4(Address2);
                            break;                       
                        default:
                            break;
                    }

                    _log.Write(SelectedMode.Title);
                    _log.Write("Проверка ТА1004М1 начата");

                    await _model.StartAsync(_token);

                    _canStart = true;
                    RaisePropertyChanged(nameof(StartButtonText));
                    ProgressBarVisibility = Visibility.Hidden;
                    RaisePropertyChanged(nameof(ProgressBarVisibility));
                    IsPanelEnabled = true;
                    RaisePropertyChanged(nameof(IsPanelEnabled));

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = true });
                    
                    if (_model as PassingInformationModel != null)
                    {
                        _log.Write($"{ResultMessage}");
                    }

                    _log.Write("Проверка ТА1004М1 завершена\n");     
                }
                else
                {
                    _cancelTokenSource.Cancel();
                }
            });

        private void ModelCodeBits_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CodeBits")
                RaisePropertyChanged("CodeBitsResult");
        }
        private void ModelResult_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Result")
                RaisePropertyChanged("ResultMessage");
        }
    }

    class TA1004M1TestMode
    {
        public TA1004M1TestMode(string title, NumbersOfModes modeNum)
        {
            Title = title;
            ModeNum = modeNum;
            Margin = new Thickness(0, 0, 0, 10);
        }

        public string Title { get; }
        public NumbersOfModes ModeNum { get; }
        public Thickness Margin { get; set; }
    }

    public enum NumbersOfModes
    {
        /// <summary>
        /// Проверка тока потребления.
        /// </summary>
        Mode1 = 1,
        /// <summary>
        /// Проверка прохождения информации.
        /// </summary>
        Mode2 = 2,
        /// <summary>
        /// Проверка адресатора.
        /// </summary>
        Mode3 = 3,
        /// <summary>
        /// Формирование ЗИ и Тоср.
        /// </summary>
        Mode4 = 4,
        /// <summary>
        /// Измерение параметров сигналов Тоср.
        /// </summary>
        Mode5 = 5,
        /// <summary>
        ///  Измерение параметров сигналов ЗИ1 и ЗИ2.
        /// </summary>
        Mode6 = 6,
        /// <summary>
        ///  Измерение параметров сигналов ЗИ3 и ЗИ4.
        /// </summary>
        Mode7 = 7,
        /// <summary>
        ///  Проверка входов информации.
        /// </summary>
        Mode8 = 8,
        /// <summary>
        /// Проверка разрядов кода.
        /// </summary>
        Mode9 = 9,
        /// <summary>
        /// Проверка задержки выдачи на АУ1.
        /// </summary>
        Mode10 = 10,
        /// <summary>
        /// Проверка задержки выдачи на АУ2.
        /// </summary>
        Mode11 = 11,
    }
}
