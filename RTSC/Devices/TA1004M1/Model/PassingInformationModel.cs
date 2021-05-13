using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RTSC.Devices.Helpers;
using System.Windows.Media;
using RTSC.DialogWindows;
using System;
using System.ComponentModel;
using System.Windows;

namespace RTSC.Devices.TA1004M1.Model
{
    class PassingInformationModel : INotifyPropertyChanged, IModel
    {
        private FtdiDevice[] _devices;
        private List<byte> _txA;
        private List<byte> _txB;
        private List<byte[]> _rxA;
        private List<byte[]> _rxB;
        private TK158Settings _tK158Settings;
        private byte _delay;
        private int _errors;
        private string _result;
       
        public PassingInformationModel()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) => _delay = o.TA1004M1Delay);

            PassingInformation = new List<PassingInformationContent>()
            {
                new PassingInformationContent(0, "00000001", null),
                new PassingInformationContent(1, "00000010", null),
                new PassingInformationContent(2, "00000011", null),
                new PassingInformationContent(3, "Счетчик", null),
                new PassingInformationContent(4, "10101100", null),
                new PassingInformationContent(5, "10101100", null),
                new PassingInformationContent(6, "10101100", null),
                new PassingInformationContent(7, "10101100", null),
            };

            _txA = new List<byte>();
            _txB = new List<byte>();
            _rxA = new List<byte[]>();
            _rxB = new List<byte[]>();
            _delay = Properties.Settings.Default.TA1004M1Delay;
        }

        public string Result
        {
            get => _result;
            set
            {
                this.MutateVerbose(ref _result, value, args => PropertyChanged?.Invoke(this, args));
            }
        }       
        public List<PassingInformationContent> PassingInformation { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Начать проверку асинхронно.
        /// </summary>
        /// <param name="token">Токен для отмены проверки.</param>
        public async Task StartAsync(CancellationToken token)
        {

#if DEBUG
            Console.WriteLine("\n*** TA1004M1 Data ***");

            for (int i = 0; i < _txA.Count; i++)
            {
                Console.WriteLine($"\tA: {Convert.ToString(_txA[i], 2).PadLeft(8, '0')} B: {Convert.ToString(_txB[i], 2).PadLeft(8, '0')}");
            }
#endif

            if (_devices != null && _devices[0].IsDeviceOpen == true && _devices[1].IsDeviceOpen == true)
            {
                for (int i = 0; i < _devices.Length; i++)
                {
                    _devices[0].Purge();
                }

                // Задержка
                for (int i = 0; i < _delay; i++)
                {
                    await _devices[0].WriteByteAsync(_txA[0]).ContinueWith(t => _txA.MoveFirstItemToEnd());
                    await _devices[1].WriteByteAsync(_txB[0]).ContinueWith(t => _txB.MoveFirstItemToEnd());
                    await _devices[0].ReadBytesAsync().ContinueWith(t => _devices[1].ReadBytesAsync());
                }

                for (int i = 0; i < _txA.Count; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    await _devices[0].WriteByteAsync(_txA[i]).ContinueWith(t => _devices[1].WriteByteAsync(_txB[i]));

                    if (_tK158Settings.WordLength == WordLength._32Bit)
                    {
                        await _devices[0].WriteByteAsync(0x0).ContinueWith(t => _devices[1].WriteByteAsync(0x0));
                    }

                    await _devices[0].ReadBytesAsync().ContinueWith(t => _rxA.Add(t.Result));
                    await _devices[1].ReadBytesAsync().ContinueWith(t => _rxB.Add(t.Result));

                    if (_rxA.Count == PassingInformation.Count)
                    {
                        for (int j = 0; j < PassingInformation.Count; j++)
                        {
                            PassingInformation[j].Result = _rxA[j][0];
                            if (j != 3)
                            {
                                if (PassingInformation[j].Result != Convert.ToByte(PassingInformation[j].Denomination, 2))
                                {
                                    PassingInformation[j].Color = Constants.RED_COLOR;
                                    _errors++;
                                }
                                else
                                {
                                    PassingInformation[j].Color = Constants.BODY_THEME_COLOR;
                                }
                            }
                        }
                        _rxA.Clear();
                        _rxB.Clear();
                    }
                }

                Result = _errors > 0 ? "Проверка прошла с ошибками" : "Проверка прошла без ошибок";
            }
#if DEBUG
            else
            {
                await Task.Run(() =>
                {
                    // Задержка
                    for (int i = 0; i < _delay; i++)
                    {
                        _txA.MoveFirstItemToEnd();
                        _txB.MoveFirstItemToEnd();
                    }

                    while (!token.IsCancellationRequested)
                    {
                        for (int i = 0, j = 0; i < _txA.Count; i++, j++)
                        {
                            if (token.IsCancellationRequested)
                            { 
                                break;
                            }

                            if (j == PassingInformation.Count)
                            {
                                j = 0;
                            }

                            PassingInformation[j].Result = (byte)(_txA[i] | _txB[i]);

                            if (j != 3)
                            {
                                if (PassingInformation[j].Result != Convert.ToByte(PassingInformation[j].Denomination,2))
                                {
                                    PassingInformation[j].Color = Constants.RED_COLOR;
                                }
                            }
                        }
                    }
                });
            }
#endif
        }
        
        /// <summary>
        /// Подготовка данных для режима "Проверка прохождения информации".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        /// <param name="address2">Адрес второго устройства.</param>
        public void Prepare(string address1, string address2)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);
            byte addr2 = (byte)(Convert.ToByte(address2, 2) << 4);

            _txA.Clear();
            _txB.Clear();
            _rxA.Clear();
            _rxB.Clear();
            _errors = 0;
            Result = string.Empty;

            foreach (var item in PassingInformation)
            {
                item.Result = null;
            }


            for (int i = 0; i < 30; i++)
            {
                _txA.Add(addr1);
                _txB.Add(Constants.PHASING);

                for (int j = 0; j < 5; j++)
                {
                    _txA.Add(addr1);
                    _txB.Add(0x0);
                }

                _txA.Add(addr2);
                _txB.Add(Constants.PHASING);

                _txA.Add(addr2);
                _txB.Add(0x0);
            }
            
        }
    }

    class PassingInformationContent : INotifyPropertyChanged
    {
        private byte? _result;
        private Brush _color;
        public PassingInformationContent(byte address, string denomination, byte? result)
        {
            Address = address;
            Denomination = denomination;
            Result = result;
            Color = Constants.BODY_THEME_COLOR;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public byte Address { get; private set; }
        public string Denomination { get; private set; }
        public byte? Result
        {
            get => _result;
            set => this.MutateVerbose(ref _result, value, args => PropertyChanged?.Invoke(this, args));
        }
        public Brush Color
        {
            get => _color;
            set => this.MutateVerbose(ref _color, value, args => PropertyChanged?.Invoke(this, args));
        }
    }
}
