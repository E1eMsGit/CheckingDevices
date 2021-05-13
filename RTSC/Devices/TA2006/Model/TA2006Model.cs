using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;
using System.Xml.Serialization;
using RTSC.Devices.Helpers;
using RTSC.DialogWindows;

namespace RTSC.Devices.TA2006.Model
{
    class TA2006Model : IModel, INotifyPropertyChanged
    {
        private FtdiDevice[] _devices;
        private TK158Settings _tK158Settings;
        private byte _delay;
        private short[] _channels;
        private List<TA2006ChannelContent> _firstCommChannels;
        private List<TA2006ChannelContent> _secondCommChannels;
        private List<TA2006ChannelContent> _thirdCommChannels;
        private List<TA2006ChannelContent> _channelsContent;
        private List<TA2006ChannelContent> _allChannelsData;
        private List<byte> _txA;
        private List<byte> _txB;
        private List<byte[]> _rxA;
        private List<byte[]> _rxB;
        private XmlSerializer _serializer;
        private string _fileName;

        public TA2006Model()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) =>
            {
                _delay = o.TA2006Delay;
            });

            _channels = new short[32];            
            _channelsContent = new List<TA2006ChannelContent>();
            _firstCommChannels = new List<TA2006ChannelContent>();
            _secondCommChannels = new List<TA2006ChannelContent>();
            _thirdCommChannels = new List<TA2006ChannelContent>();
            _allChannelsData = new List<TA2006ChannelContent>();
            _delay = Properties.Settings.Default.TA2006Delay;
            _serializer = new XmlSerializer(typeof(List<TA2006ChannelContent>));
            _fileName = @".\TA2006ChannelsParams.xml";
            _txA = new List<byte>();
            _txB = new List<byte>();
            _rxA = new List<byte[]>();
            _rxB = new List<byte[]>();

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = (short)(i << 4);
            }

            if (File.Exists(_fileName))
            {
                using (Stream reader = new FileStream(_fileName, FileMode.Open))
                {
                    _allChannelsData = (List<TA2006ChannelContent>)_serializer.Deserialize(reader);
                    _allChannelsData.ForEach(o => o.PropertyChanged += ChannelParams_PropertyChanged);

                    _firstCommChannels.AddRange(_allChannelsData.Where(o => o.Number <= 32));
                    _secondCommChannels.AddRange(_allChannelsData.Where(o => o.Number >= 33 && o.Number <= 64));
                    _thirdCommChannels.AddRange(_allChannelsData.Where(o => o.Number >= 65));
                }            
            }
            else
            {
                for (int i = 0; i < _channels.Length; i++)
                {
                    _firstCommChannels.Add(new TA2006ChannelContent() { Number = i + 1, Tolerance = 1 });
                    _secondCommChannels.Add(new TA2006ChannelContent() { Number = i + 33, Tolerance = 1 });
                    _thirdCommChannels.Add(new TA2006ChannelContent() { Number = i + 65, Tolerance = 1 });

                    _firstCommChannels[i].PropertyChanged += ChannelParams_PropertyChanged;
                    _secondCommChannels[i].PropertyChanged += ChannelParams_PropertyChanged;
                    _thirdCommChannels[i].PropertyChanged += ChannelParams_PropertyChanged;
                }

                _allChannelsData.AddRange(_firstCommChannels);
                _allChannelsData.AddRange(_secondCommChannels);
                _allChannelsData.AddRange(_thirdCommChannels);

                using (StreamWriter writer = new StreamWriter(_fileName))
                {
                    _serializer.Serialize(writer, _allChannelsData);
                }
            }
        }

        public List<TA2006ChannelContent> ChannelContent
        {
            get => _channelsContent;
            set
            {
                this.MutateVerbose(ref _channelsContent, value, args => PropertyChanged?.Invoke(this, args));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public async Task StartAsync(CancellationToken token)
        {
#if DEBUG
            byte commNum = 0;
            Console.WriteLine("\n*** TA2006 Data ***");
            for (int i = 0; i < _txA.Count; i++)
            {
                if (i % 32 == 0)
                {
                    commNum++;
                    Console.WriteLine($"Commutator: {commNum}");
                }
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

                        await _devices[0].ReadBytesAsync()
                            .ContinueWith(t => {
                                _rxA.Add(t.Result);

                                if (_rxA.Count == 7)
                                {
                                    Task.Run(() => SetDenominations(_rxA[6][0], 0, _allChannelsData.Count / 2));
                                }
                                else if (_rxA.Count == 55)
                                {
                                    Task.Run(() => SetDenominations(_rxA[54][0], _allChannelsData.Count / 2, _allChannelsData.Count));
                                }
                            })
                            .ContinueWith(t => _devices[1].ReadBytesAsync());

                        _allChannelsData[i].Value = _rxA[i][0];
                        _allChannelsData[i].FontColor =
                           (_allChannelsData[i].Value != 255 &&
                            _allChannelsData[i].Value > _allChannelsData[i].Denomination + _allChannelsData[i].Tolerance) ||
                            _allChannelsData[i].Value < _allChannelsData[i].Denomination - _allChannelsData[i].Tolerance 
                            ? 
                            _allChannelsData[i].FontColor = Constants.RED_COLOR : _allChannelsData[i].FontColor = Constants.BODY_THEME_COLOR;

                        
                    }
                    _rxA.Clear();
                    _rxB.Clear();
                }
            }
#if DEBUG
            else
            {
                Random rd = new Random();
                await Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        for (int i = 0; i < _allChannelsData.Count; i++)
                        {
                            byte val = (byte)rd.Next(254, 255);

                            if (i == 7)
                            {
                                SetDenominations(val, 0, _allChannelsData.Count / 2);
                            }
                            else if (i == 55)
                            {
                                SetDenominations(val, _allChannelsData.Count / 2, _allChannelsData.Count);
                            }

                            _allChannelsData[i].Value = val;
                            _allChannelsData[i].FontColor =
                                 (_allChannelsData[i].Value != 255 &&
                                _allChannelsData[i].Value > _allChannelsData[i].Denomination + _allChannelsData[i].Tolerance) || 
                                _allChannelsData[i].Value < _allChannelsData[i].Denomination - _allChannelsData[i].Tolerance ? 
                                _allChannelsData[i].FontColor = Constants.RED_COLOR : _allChannelsData[i].FontColor = Constants.BODY_THEME_COLOR; 
                        }

                        Thread.Sleep(100);
                    }
                });
            }
#endif
        }

        private void SetDenominations(byte val, int beginInd, int endInd)
        {
            byte[] denominations = new byte[] {
                (byte)(val * 0), (byte)(val * 0.2),
                (byte)(val * 0.3), (byte)(val * 0.4), 
                (byte)(val * 0.6), (byte)(val * 0.8),
                val 
            };

            for (int i = beginInd; i < endInd; i += 7)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (j + i == endInd)
                    {
                        break;
                    }

                    _allChannelsData[j + i].Denomination = denominations[j];
                }
            }
        }
        public void ChangeCommutator(byte commNum)
        {
            ChannelContent = commNum == 1 ? _firstCommChannels : commNum == 2 ? _secondCommChannels : _thirdCommChannels;
        }
        public void Prepare(byte address)
        {
            short addr = (short)(address << 12); // xxxx0000 00000000
            short firstCommNum = 512; // 00000010 00000000 
            short[] data = new short[96];

            _txA.Clear();
            _txB.Clear();
            _rxA.Clear();
            _rxB.Clear();

            for (int i = 1; i <= 3; i++)
            {
                for (int j = 0; j < _channels.Length; j++)
                {
                    data[j * i] = (short)(addr | firstCommNum | _channels[j]);
                    _txA.Add((byte)(data[j * i] >> 8));
                    _txB.Add((byte)data[j * i]);
                }
                firstCommNum = (short)(firstCommNum << 1);
            }
        }
        private void ChannelParams_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Tolerance")
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_fileName))
                    {
                        _serializer.Serialize(writer, _allChannelsData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(new string('*', 10));
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(new string('*', 10));
                }

            }
        }
    }

    [Serializable]
    public class TA2006ChannelContent : INotifyPropertyChanged
    {
        private byte _value; // Значение.
        private byte _denomination; // Номинал.
        private byte _tolerance;    // Допуск.
        private Brush _fontColor; // Цвет шрифта значения.

        public TA2006ChannelContent()
        {
            Value = 0;
            Denomination = 0;
            FontColor = Constants.BODY_THEME_COLOR;
        }

        public int Number { get; set; }
        [XmlIgnore]
        public byte Value
        {
            get => _value;
            set => this.MutateVerbose(ref _value, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public byte Denomination
        {
            get => _denomination;
            set => this.MutateVerbose(ref _denomination, value, args => PropertyChanged?.Invoke(this, args));
        }
        public byte Tolerance
        {
            get => _tolerance;
            set => this.MutateVerbose(ref _tolerance, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush FontColor
        {
            get => _fontColor;
            set => this.MutateVerbose(ref _fontColor, value, args => PropertyChanged?.Invoke(this, args));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
