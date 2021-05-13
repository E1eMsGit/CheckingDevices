using GalaSoft.MvvmLight.Messaging;
using System.Threading;
using System.Threading.Tasks;
using RTSC.Devices.Helpers;
using RTSC.DialogWindows;
using System.Collections.Generic;
using System;

namespace RTSC.Devices.TA1004M1.Model
{
    class OtherTestsModel : IModel
    {      
        private FtdiDevice[] _devices;
        private List<byte> _txA;
        private List<byte> _txB;
        private TK158Settings _tK158Settings;
        private byte _delay;

        public OtherTestsModel()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) => _delay = o.TA1004M1Delay);

            _txA = new List<byte>();
            _txB = new List<byte>();
            _delay = Properties.Settings.Default.TA1004M1Delay;
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

                        await _devices[0].ReadBytesAsync().ContinueWith(t => _devices[1].ReadBytesAsync());
                    }
                }

                _txA.Clear();
                _txB.Clear();               
            }
        }

        /// <summary>
        /// Подготовка данных для режима "Проверка тока потребления".
        /// </summary>       
        public void PrepareMode1()
        {
            _txA.Add(0x0);
            _txB.Add(Constants.PHASING);
        }
        /// <summary>
        /// Подготовка данных для режима "Проверка адресатора".
        /// </summary>
        public void PrepareMode2()
        {
            for (byte i = 0; i < 16; i++)
            {
                _txA.Add((byte)(i << 4));
                _txB.Add(Constants.PHASING);              
            }
        }
        /// <summary>
        /// Подготовка данных для режима "Формирование ЗИ и Тоср".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        /// <param name="address2">Адрес второго устройства.</param>
        public void PrepareMode3(string address1, string address2)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);
            byte addr2 = (byte)(Convert.ToByte(address2, 2) << 4);

            _txA.Add(addr1);
            _txB.Add(Constants.PHASING);
            
            for (int i = 1; i < 6; i++)
            {
                _txA.Add(addr1);
                _txB.Add(0x0);
            }

            _txA.Add(addr2);
            _txB.Add(Constants.PHASING);
            
            _txA.Add(addr2);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Подготовка данных для режима "Измерение параметров сигналов Тоср".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        public void PrepareMode4(string address1)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);

            _txA.Add(addr1);
            _txB.Add(Constants.PHASING);
        }
        /// <summary>
        /// Подготовка данных для режима "Измерение параметров сигналов ЗИ1 и ЗИ2".
        /// </summary>
        /// <param name="address1">Адрес первого устройства.</param>
        public void PrepareMode5(string address1)
        {
            byte addr1 = (byte)(Convert.ToByte(address1, 2) << 4);

            _txA.Add(addr1);
            _txB.Add(0x0);
        }
        /// <summary>
        /// Подготовка данных для режима "Измерение параметров сигналов ЗИ3 и ЗИ4".
        /// </summary>
        /// <param name="address2">Адрес второго устройства.</param>
        public void PrepareMode6(string address2)
        {
            byte addr2 = (byte)(Convert.ToByte(address2, 2) << 4);

            _txA.Add(addr2);
            _txB.Add(Constants.PHASING);

            _txA.Add(addr2);
            _txB.Add(0x0);

            _txA.Add(addr2);
            _txB.Add(0x0);
        }
    }
}
