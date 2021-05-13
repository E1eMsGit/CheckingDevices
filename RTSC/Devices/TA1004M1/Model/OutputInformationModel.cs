using GalaSoft.MvvmLight.Messaging;
using System.Collections.Generic;
using RTSC.Devices.Helpers;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using RTSC.DialogWindows;
using System;

namespace RTSC.Devices.TA1004M1.Model
{
    class OutputInformationModel :  INotifyPropertyChanged, IModel
    {
        private FtdiDevice[] _devices;
        private List<byte> _txA;
        private List<byte> _txB;
        private List<byte[]> _rxA;
        private List<byte[]> _rxB;
        private byte _modeNumber;
        private byte? _codeBits;
        private TK158Settings _tK158Settings;
        private byte _delay;

        public OutputInformationModel()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) => _delay = o.TA1004M1Delay);

            _txA = new List<byte>();
            _txB = new List<byte>();
            _rxA = new List<byte[]>();
            _rxB = new List<byte[]>();
            _delay = Properties.Settings.Default.TA1004M1Delay;

            OutputInformation = new List<OutputInformationContent>()
            {
                new OutputInformationContent(0, null),
                new OutputInformationContent(1, null),
                new OutputInformationContent(2, null),
                new OutputInformationContent(3, null),
                new OutputInformationContent(4, null),
                new OutputInformationContent(5, null),
                new OutputInformationContent(6, null),
                new OutputInformationContent(7, null),
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public List<OutputInformationContent> OutputInformation { get; }
        public byte? CodeBits
        {
            get => _codeBits;
            set
            {
                this.MutateVerbose(ref _codeBits, value, args => PropertyChanged?.Invoke(this, args));
            }
        }

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

                while (!token.IsCancellationRequested)
                {
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

                        await _devices[0].ReadBytesAsync().ContinueWith(t => { if (_modeNumber == 1 || _modeNumber == 2) _rxA.Add(t.Result); });
                        await _devices[1].ReadBytesAsync().ContinueWith(t => { if (_modeNumber == 1 || _modeNumber == 2) _rxB.Add(t.Result); });

                        // "Входы информации".
                        if (_modeNumber == 1)
                        {
                            if (_rxA.Count == OutputInformation.Count)
                            {
                                for (int j = 0; j < OutputInformation.Count; j++)
                                {
                                    // Тут какая то логика по получению значения из А (из _rxA[j][0]).
                                    // Тут какая то логика по получению значения из В (из _rxB[j][0]).
                                    // Тут надо сложить полученные значения из А и В (А | В) и записать в OutputInformation[j].Data (или нет).
                                    OutputInformation[j].Data = _rxA[j][0];
                                }
                                _rxA.Clear();
                                _rxB.Clear();
                            }
                        }
                        // "Разряды кода".
                        else if (_modeNumber == 2)
                        {
                            // Тут какая то логика по получению значения из А.
                            // Тут какая то логика по получению значения из В.
                            // Тут надо сложить полученные значения из А и В (А | В) и записать в CodeBits (или нет).
                            CodeBits = _rxA[i][0];
                            _rxA.Clear();
                            _rxB.Clear();
                        }
                    }
                }

                _txA.Clear();
                _txB.Clear();
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
                        for (int i = 0; i < _txA.Count; i++)
                        {
                            if (token.IsCancellationRequested)
                            {
                                break;
                            }

                            if (_modeNumber == 1)
                            {
                                OutputInformation[i].Data = (byte)(_txA[i] | _txB[i]);
                            }
                            else if (_modeNumber == 2)
                            {
                                CodeBits = (byte)(_txA[i] | _txB[i]);
                            }
                        }
                    }

                    _txA.Clear();
                    _txB.Clear();
                });
            }
#endif
        }

        /// <summary>
        /// Подготовка данных для режима "Входы информации".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        /// <param name="address2">Адрес второго устройства.</param>
        public void PrepareMode1(string address1, string address2)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);
            byte addr2 = (byte)(Convert.ToByte(address2, 2) << 4);

            Init(1);
            foreach (var item in OutputInformation)
            {
                item.Data = null;
            }

            _txA.Add(addr1);
            _txB.Add(Constants.PHASING);

            for (int i = 1; i < 5; i++)
            {
                _txA.Add(addr1);
                _txB.Add(0x0);
            }

            _txA.Add(addr2);
            _txB.Add(Constants.PHASING);

            _txA.Add(addr2);
            _txB.Add(0x0);

            _txA.Add(addr1);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Подготовка данных для режима "Разряды кода".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        public void PrepareMode2(string address1)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);

            Init(2);
            CodeBits = null;

            _txA.Add(addr1);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Подготовка данных для режима "Задержка выдачи на АУ1".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        public void PrepareMode3(string address1)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);

            Init(3);

            _txA.Add(addr1);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Подготовка данных для режима "Задержка выдачи на АУ2".
        /// </summary>
        /// <param name="address2">Адрес второго устройства.</param>
        public void PrepareMode4(string address2)
        {
            byte addr2 = (byte)(Convert.ToByte(address2, 2) << 4);

            Init(4);

            _txA.Add(addr2);
            _txB.Add(Constants.PHASING);

            _txA.Add(addr2);
            _txB.Add(0x0);

            _txA.Add(addr2);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Сброс устройств и очистка списков.
        /// </summary>
        private void Init(byte modeNumber)
        {
            _modeNumber = modeNumber;
            _rxA.Clear();
            _rxB.Clear();
        }        
    }

    class OutputInformationContent : INotifyPropertyChanged
    {
        private byte? _data;

        public OutputInformationContent(byte address, byte? data)
        {
            Address = address;
            Data = data;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public byte Address { get; private set; }
        public byte? Data
        {
            get => _data;
            set => this.MutateVerbose(ref _data, value, args => PropertyChanged?.Invoke(this, args));
        }
    }
}
