using System;
using System.Collections.Generic;
using RTSC.Devices.Helpers;
using RTSC.DialogWindows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
using System.Text;
using System.Threading;
using System.Windows;
using RTSC.ViewModel;

namespace RTSC.Devices.Debug.ViewModel
{
    class DebugViewModel : ViewModelBase
    {
        private DataFile _dataFile;
        private FtdiDevice[] _devices;
        private string _inputData;
        private StringBuilder _outputDataA;
        private StringBuilder _outputDataB;
        private bool _canStart;
        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _token;
        private TK158Settings _tK158Settings;
        private bool _isInfiniteSending;
        private bool _isArraySending;
        private string _fileName;
        private Visibility _closeFileVisibility;

        public DebugViewModel()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => {
                _devices = o;
                RaisePropertyChanged(nameof(DeviceList));
            });
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);

            _canStart = true;
            _inputData = string.Empty;
            _outputDataA = new StringBuilder();
            _outputDataB = new StringBuilder();

            CloseFileVisibility = Visibility.Hidden;
        }

        public string FileName
        {
            get => _fileName;
            set => Set(ref _fileName, value);
        }
        public Visibility CloseFileVisibility
        {
            get => _closeFileVisibility;
            set => Set(ref _closeFileVisibility, value);
        }
        public string InputData
        {
            get => _inputData;
            set => Set(ref _inputData, value);
        }
        public string OutputDataA
        {
            get => _outputDataA.ToString();
            set 
            {
                _outputDataA.Append(value);
                RaisePropertyChanged();
            }
        }
        public string OutputDataB
        {
            get => _outputDataB.ToString();
            set
            {
                _outputDataB.Append(value);
                RaisePropertyChanged();
            }
        }
        public bool IsInfiniteSending 
        { 
            get => _isInfiniteSending;
            set => Set(ref _isInfiniteSending, value); 
        }
        public bool IsArraySending
        {
            get => _isArraySending;
            set => Set(ref _isArraySending, value);
        }
        public IEnumerable<FtdiDevice> DeviceList => _devices;

        public RelayCommand OpenFileCommand => new RelayCommand(
            () =>
            {
                _dataFile = new DataFile();
                _dataFile.Open();
                
                if (_dataFile.DialogResult == true)
                {
                    FileName = _dataFile.FileName;
                    CloseFileVisibility = Visibility.Visible;

                    if (_dataFile != null && _dataFile.FileData != null)
                    {
                        InputData = _dataFile.FileData;
                    }
                    else
                    {
                        InputData = string.Empty;
                    }
                }
            });
        public RelayCommand CloseFileCommand => new RelayCommand(
            () =>
            {
                _dataFile = null;
                FileName = string.Empty;
                InputData = string.Empty;
                CloseFileVisibility = Visibility.Hidden;
            });
        public RelayCommand WriteReadDeviceCommand => new RelayCommand(
            async () =>
            {
                if (_devices != null)
                {               
                    if (_canStart)
                    {
                        _canStart = false;
                        _outputDataA.Clear();
                        _outputDataB.Clear();

                        byte[] massA = null;
                        byte[] massB = null;

                        _cancelTokenSource = new CancellationTokenSource();
                        _token = _cancelTokenSource.Token;

                        Messenger.Default.Send(new HeaderEnable { IsEnabled = false });

                        for (int i = 0; i < _devices.Length; i++)
                        {
                            _devices[i].Purge();
                        }

                        try
                        {
                            var _dataForSend = GetArrayFromHexString(InputData);

                            if (_dataForSend.Length % 2 == 0)
                            {
                                massA = PrepareData(_dataForSend, 0);
                                massB = PrepareData(_dataForSend, 1);
                            }
                            else
                            {
                                Dialog.ShowNotificationDialog("Ошибка", "Введите четное количество символов");
                            }

                            if (massA != null && massB != null)
                            {
                                while (!_canStart)
                                {
                                    for (int i = 0; i < massA.Length; i++)
                                    {
                                        if (_token.IsCancellationRequested)
                                        {
                                            break;
                                        }

                                        if (IsArraySending)
                                        {
                                            await _devices[0].WriteBytesAsync(massA).ContinueWith(t => _devices[1].WriteBytesAsync(massB));
                                        }
                                        else
                                        {
                                            await _devices[0].WriteByteAsync(massA[i]).ContinueWith(t => _devices[1].WriteByteAsync(massB[i]));
                                        }
                                        
                                        if (_tK158Settings.WordLength == WordLength._32Bit)
                                        {
                                            await _devices[0].WriteByteAsync(0x0).ContinueWith(t => _devices[1].WriteByteAsync(0x0));
                                        }

                                        await _devices[0].ReadBytesAsync().ContinueWith(t => OutputDataA = $"{BitConverter.ToString(t.Result).Replace("-", " ")} ");
                                        await _devices[1].ReadBytesAsync().ContinueWith(t => OutputDataB = $"{BitConverter.ToString(t.Result).Replace("-", " ")} ");
                                    }

                                    if (!IsInfiniteSending)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                        catch (FormatException)
                        {
                            Dialog.ShowNotificationDialog("Ошибка", "Некорректный ввод");
                        }
                    }

                    _canStart = true;

                    _cancelTokenSource.Cancel();

                    Messenger.Default.Send(new HeaderEnable { IsEnabled = true });
                }

            }, () => InputData.Length > 0);

        /// <summary>
        /// Преобразование строки данных (hex) в массив байт.
        /// </summary>
        /// <param name="data">Данные из файла или с тестовой View.</param>
        /// <returns>Данные из файла в виде массива.</returns>
        public static byte[] GetArrayFromHexString(string data)
        {
            if (data == string.Empty)
            {
                return null;
            }

            string[] strData = data.Trim(' ').Split(' ');
            byte[] dataForSend = new byte[strData.Length];

            for (int bytesCounter = 0; bytesCounter < strData.Length; bytesCounter++)
            {
                dataForSend[bytesCounter] = Convert.ToByte(strData[bytesCounter], 16);
            }

            return dataForSend;
        }
        /// <summary>
        /// Получение массива данных для отправки на устройство.
        /// </summary>
        /// <param name="data">Преобразованные в массив байт данные из View (полученные из метода PrepareDataForSend).</param>
        /// <param name="index">С какого места начинать считывать данные из data.</param>
        /// <returns>Данные для отправки на устройство.</returns>
        private byte[] PrepareData(byte[] data, int index)
        {
            int arraySize = data.Length / 2;
            byte[] dataArray = new byte[arraySize];

            for (int i = 0, j = index; i < arraySize; i++, j += 2)
            {
                dataArray[i] = data[j];
            }

            return dataArray;
        }
    }
}