using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RTSC.Devices.Helpers;
using RTSC.DialogWindows;

namespace RTSC.Devices.TA2008.Model
{
    class TA2008Model : IModel
    {
        private FtdiDevice[] _devices;
        private TK158Settings _tK158Settings;
        private byte _delay;
        List<byte> _txA;
        List<byte> _txB;
        private bool _isStart;

        public TA2008Model()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) => _delay = o.TA2008Delay);

            Channels = new List<List<TA2008ChannelContent>>();
            _txA = new List<byte>();
            _txB = new List<byte>();

            for (byte i = 1; i <= 96; i+=8)
            {
                var channel = new List<TA2008ChannelContent>();
                
                for (byte j = 0; j < 8; j++)
                {
                    channel.Add(new TA2008ChannelContent((byte)(i + j)));
                }

                Channels.Add(channel);
            }
            _delay = Properties.Settings.Default.TA2008Delay;
        }

        public List<List<TA2008ChannelContent>> Channels { get; }

        public async Task StartAsync(CancellationToken token)
        {
#if DEBUG
            Console.WriteLine("\n*** TA2008 Data ***");

            for (int i = 0; i < _txA.Count; i++)
            {
                Console.WriteLine($"\tA: {Convert.ToString(_txA[i], 2).PadLeft(8, '0')} B: {Convert.ToString(_txB[i], 2).PadLeft(8, '0')}");
            }
#endif
            if (_devices != null && _devices[0].IsDeviceOpen == true && _devices[1].IsDeviceOpen == true)
            {
                _isStart = true;

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

                        await _devices[0].ReadBytesAsync()
                            .ContinueWith(t => {
                                byte[] bits = Convert.ToString(t.Result[0], 2).PadLeft(8, '0')
                                .Select(c => byte.Parse(c.ToString()))
                                .ToArray();
                                
                                for (int j = 0; j < bits.Length; j++)
                                {
                                    Channels[i][j].Value = bits[j];
                                }})
                            .ContinueWith(t => _devices[1].ReadBytesAsync());                  
                    }
                }
            }
#if DEBUG
            else
            {
                await Task.Run(() =>
                {
                    _isStart = true;
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
                            byte[] bits = Convert.ToString(_txA[i], 2).PadLeft(8, '0')
                                .Select(c => byte.Parse(c.ToString()))
                                .ToArray();

                            for (int j = 0; j < bits.Length; j++)
                            {
                                Channels[i][j].Value = bits[j];
                            }          
                        }
                    }
                });
            }
#endif
            _isStart = false;
        }

        public void Prepare(byte address, byte selectedChannels)
        {
            short addr = (short)(address << 12);
            short[] commands = new short[12];
            short firstNumOfEights = 1;

            _txA.Clear();
            _txB.Clear();

            for (byte i = 0; i < selectedChannels; i++)
            {
                commands[i] = (short)(addr | firstNumOfEights);
                firstNumOfEights <<= 1;
            }

            for (byte i = 0; i < selectedChannels; i++)
            {
                _txA.Add((byte)(commands[i] >> 8));
                _txB.Add((byte)commands[i]);
            }

            if (_isStart)
            {
                for (int i = 0; i < _delay; i++)
                {
                    _txA.MoveFirstItemToEnd();
                    _txB.MoveFirstItemToEnd();
                }
            }
        }
    }

    class TA2008ChannelContent : INotifyPropertyChanged
    {
        private byte? _value;

        public TA2008ChannelContent(byte address)
        {
            Address = address;
            Value = null;
        }

        public byte Address { get; private set; }
        public byte? Value
        {
            get => _value;
            set => this.MutateVerbose(ref _value, value, args => PropertyChanged?.Invoke(this, args));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
