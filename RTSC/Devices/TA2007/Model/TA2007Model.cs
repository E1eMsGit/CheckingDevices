using GalaSoft.MvvmLight.Messaging;
using MaterialDesignColors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;
using RTSC.Devices.Helpers;
using RTSC.Devices.TA2007.ViewModel;
using RTSC.DialogWindows;

namespace RTSC.Devices.TA2007.Model
{
    class TA2007Model : IModel
    {
        private FtdiDevice[] _devices;
        private TK158Settings _tK158Settings;
        private List<int> _selectedChannelsList;
        private List<byte> _txA;
        private List<byte> _txB;
        private byte[] _rxA;
        private byte[] _rxB;
        private short[] _channels;
        private byte _delay;
        private string _fileName;
        private XmlSerializer _serializer;
        private int _timerValue;
        private TA2007TestMode _testMode;

        private double _errorCalculationValue;
        private double _errorCalculationResult;
        private short[] _results; // Для сохранения значений из нулевого и выбранного канала.

        public TA2007Model()
        {
            Messenger.Default.Register<FtdiDevice[]>(this, (o) => _devices = o);
            Messenger.Default.Register<TK158Settings>(this, (o) => _tK158Settings = o);
            Messenger.Default.Register<DebugSettings>(this, (o) =>
            {
                _delay = o.TA2007Delay;
                _timerValue = o.TA2007TimerValue;
            });
  
            _delay = Properties.Settings.Default.TA2007Delay;
            _timerValue = Properties.Settings.Default.TA2007TimerValue;
            _serializer = new XmlSerializer(typeof(List<TA2007ChannelContent>));
            _fileName = @".\TA2007ChannelsParams.xml";
            _channels = new short[32];

            ChannelContent = new List<TA2007ChannelContent>();
            if (File.Exists(_fileName))
            {
                using (Stream reader = new FileStream(_fileName, FileMode.Open))
                {
                    ChannelContent = (List<TA2007ChannelContent>)_serializer.Deserialize(reader);
                    ChannelContent.ForEach(o => o.PropertyChanged += ChannelParams_PropertyChanged);
                }
            }
            else
            {
                for (int i = 0; i < _channels.Length; i++)
                {
                    ChannelContent.Add(new TA2007ChannelContent() { Number = i + 1, Norm = 0,  Tolerance = 0 });
                    ChannelContent[i].PropertyChanged += ChannelParams_PropertyChanged;
                }

                using (StreamWriter writer = new StreamWriter(_fileName))
                {
                    _serializer.Serialize(writer, ChannelContent);
                }
            }

            Messenger.Default.Send<Brush[]>(ChannelContent[0].PhasSyncColors);
        }

        public List<TA2007ChannelContent> ChannelContent { get; }

        public async Task StartAsync(CancellationToken token)
        {
#if DEBUG
            Console.WriteLine("\n*** TA2007 Data ***");
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
                        if (token.IsCancellationRequested && _timerValue == 0)
                        {
                            break;
                        }

                        await _devices[0].WriteByteAsync(_txA[i]).ContinueWith(t => _devices[1].WriteByteAsync(_txB[i]));

                        if (_tK158Settings.WordLength == WordLength._32Bit)
                        {
                            await _devices[0].WriteByteAsync(0x0).ContinueWith(t => _devices[1].WriteByteAsync(0x0));
                        }
                                            
                        await _devices[0].ReadBytesAsync().ContinueWith(t => _rxA = t.Result);
                        await _devices[1].ReadBytesAsync().ContinueWith(t => _rxB = t.Result);

                        short lowByteVal = (short)(_rxA[0] >> 2);
                        short highByteVal = (short)((_rxB[0] >> 2) << 6);
                        short result = (short)(lowByteVal | highByteVal);

                        int index = _selectedChannelsList != null ? _selectedChannelsList[i] - 1 : i;
                       
                        ChannelContent[index].Result = result;
                        ChannelContent[index].PhasSyncColors[0] = (_rxA[0] & 2) == 2 ? Constants.GREEN_COLOR : Constants.RED_COLOR;
                        ChannelContent[index].PhasSyncColors[1] = (_rxA[0] & 1) == 1 && (_rxB[0] & 1) == 1 ? Constants.GREEN_COLOR : Constants.RED_COLOR;
                        Messenger.Default.Send<Brush[]>(ChannelContent[index].PhasSyncColors);

                        // Сохранение результатов опроса первого и выбранного каналов в режиме проверки "Расчет погрешности".
                        if (_testMode == TA2007TestMode.ErrorCalculationResistance || _testMode == TA2007TestMode.ErrorCalculationVoltage)
                        {
                            _results[i] = result;
                        }
                    }

                    if (_testMode == TA2007TestMode.ErrorCalculationResistance)
                    {
                        var resistanceResult = CountErrorCalculationResistance();
                        _errorCalculationResult = resistanceResult > _errorCalculationResult ? resistanceResult : _errorCalculationResult;
                    }
                    else if (_testMode == TA2007TestMode.ErrorCalculationVoltage)
                    {
                        var voltageResult = CountErrorCalculationVoltage();
                        _errorCalculationResult = voltageResult > _errorCalculationResult ? voltageResult : _errorCalculationResult;
                    }

                    if (token.IsCancellationRequested && _timerValue > 0)
                    {
                        break;
                    }
                }

                StringBuilder resultText = new StringBuilder();

                for (int i = 0; i < ChannelContent.Count; i++)
                {
                    if (ChannelContent[i].IsError)
                    {
                        resultText.AppendLine($"{DateTime.Now:HH:mm:ss}. Проверка прошла с ошибками.");
                        break;
                    }
                }
                if (resultText.Length == 0)
                {
                    resultText.AppendLine($"{DateTime.Now:HH:mm:ss}. Проверка завершена.");
                }
                if (_testMode == TA2007TestMode.ErrorCalculationResistance || _testMode == TA2007TestMode.ErrorCalculationVoltage)
                {
                    resultText.AppendLine($"Результат расчета погрешности: {_errorCalculationResult} %.");
                }
                
                Messenger.Default.Send<TA2007ResultText>(new TA2007ResultText() { Text = resultText.ToString() });
            }
#if DEBUG
            else
            {
                Random rd = new Random();
                Brush[] syncColors = new Brush[] { Constants.GREEN_COLOR, Constants.RED_COLOR };
               
                await Task.Run(() =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        for (int i = 0; i < _txA.Count; i++)
                        {
                            // Если опрос бесконечный.
                            if (token.IsCancellationRequested && _timerValue == 0)
                            {
                                break;
                            }

                            sbyte val = (sbyte)rd.Next(-127, 128);
                            
                            // Для проверки расчета погрешности. Ответ -285.22
                            //sbyte val = 0;
                            //if (i == 0)
                            //{
                            //    val = 20;
                            //}
                            //else if (i == 1)
                            //{
                            //    val = 40;
                            //}

                            var index = _selectedChannelsList != null? _selectedChannelsList[i] - 1 : i;
                            
                            ChannelContent[index].Result = val;

                            // Сохранение результатов опроса первого и выбранного каналов в режиме проверки "Расчет погрешности".
                            if (_testMode == TA2007TestMode.ErrorCalculationResistance || _testMode == TA2007TestMode.ErrorCalculationVoltage)
                            {
                                _results[i] = val;
                            }

                            val = (sbyte)rd.Next(0, 2);
                            ChannelContent[i].PhasSyncColors[0] = syncColors[val];
                            val = (sbyte)rd.Next(0, 2);
                            ChannelContent[i].PhasSyncColors[1] = syncColors[val];
                            Messenger.Default.Send<Brush[]>(ChannelContent[i].PhasSyncColors);

                            Thread.Sleep(200);
                        }

                        if (_testMode == TA2007TestMode.ErrorCalculationResistance)
                        {
                            var resistanceResult = CountErrorCalculationResistance();
                            _errorCalculationResult = Math.Abs(resistanceResult) > Math.Abs(_errorCalculationResult) ? resistanceResult : _errorCalculationResult;
                        }
                        else if (_testMode == TA2007TestMode.ErrorCalculationVoltage)
                        {
                            var voltageResult = CountErrorCalculationVoltage();
                            _errorCalculationResult = Math.Abs(voltageResult) > Math.Abs(_errorCalculationResult) ? voltageResult : _errorCalculationResult;
                        }

                        // Если опрос по времени.
                        if (token.IsCancellationRequested && _timerValue > 0)
                        {
                            break;
                        }
                    }


                    StringBuilder resultText = new StringBuilder();

                    for (int i = 0; i < ChannelContent.Count; i++)
                    {
                        if (ChannelContent[i].IsError)
                        {
                            resultText.AppendLine($"{DateTime.Now:HH:mm:ss}. Проверка прошла с ошибками.");
                            break;
                        }
                    }
                    if (resultText.Length == 0)
                    {                        
                        resultText.AppendLine($"{DateTime.Now:HH:mm:ss}. Проверка завершена.");
                    }
                    if (_testMode == TA2007TestMode.ErrorCalculationResistance || _testMode == TA2007TestMode.ErrorCalculationVoltage)
                    {
                        resultText.AppendLine($"Результат расчета погрешности: {_errorCalculationResult} %.");
                    }

                    Messenger.Default.Send<TA2007ResultText>(new TA2007ResultText() { Text = resultText.ToString() });
                });
            }
#endif
        }

        /// <summary>
        /// Инициализация перед подготовкой данных для проверки.
        /// </summary>
        private void Init()
        {
            _txA = new List<byte>();
            _txB = new List<byte>();
            _rxA = new byte[1];
            _rxB = new byte[1];
            _results = new short[2];
            _errorCalculationResult = 0;

            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i] = (short)(i << 4);
            }

            for (int i = 0; i < ChannelContent.Count; i++)
            {
                ChannelContent[i].ResetValues();
            }
        }
        /// <summary>
        /// Подготовка данных для проверки функционирования каналов.
        /// </summary>
        /// <param name="address">Адрес устройства.</param>
        /// <param name="isPhasing">Флаг фазировки.</param>
        /// <param name="selectedChannelsList">Выбранные каналы.</param>
        public void FunctionCheckPrepare(byte address,  bool isPhasing, List<int> selectedChannelsList=null)
        {
            Random rnd = new Random();
            SortedSet<int> phasingIndices = new SortedSet<int>();
            _testMode = TA2007TestMode.FunctionCheck;

            short data = isPhasing? (short)(address << 12 | Constants.PHASING) : (short)(address << 12);

            Init();

            if (selectedChannelsList == null)
            {
                _selectedChannelsList = null;

                for (int i = 0; i < _channels.Length; i++)
                {
                    _channels[i] = (short)(_channels[i] | data);
                }

                // Добавляем фазировку случайное кол-во раз в случайные каналы.
                if (!isPhasing)
                {
                    var phasingCount = rnd.Next(0, 32);

                    for (int i = 0; i < phasingCount; i++)
                    {
                        phasingIndices.Add(rnd.Next(0, 32));
                    }

                    foreach (var item in phasingIndices)
                    {
                        _channels[item] = (short)(_channels[item] | Constants.PHASING);
                    }
                }

                for (int i = 0; i < _channels.Length; i++)
                {
                    _txA.Add((byte)(_channels[i] >> 8));
                    _txB.Add((byte)_channels[i]);
                }
            }
            else
            {
                _selectedChannelsList = selectedChannelsList;

                for (int i = 0; i < _selectedChannelsList.Count; i++)
                {
                    _channels[_selectedChannelsList[i] - 1] = (short)(_channels[_selectedChannelsList[i] - 1] | data);
                }

                //// Добавляем фазировку случайное кол-во раз в случайные каналы.
                if (!isPhasing)
                {
                    var phasingCount = rnd.Next(1, _selectedChannelsList.Count + 1);
                   
                    for (int i = 0; i < phasingCount; i++)
                    {
                        phasingIndices.Add(_selectedChannelsList[rnd.Next(_selectedChannelsList.Count)] - 1);
                    }

                    foreach (var item in phasingIndices)
                    {
                        _channels[item] = (short)(_channels[item] | Constants.PHASING);
                    }
                }

                for (int i = 0; i < _selectedChannelsList.Count; i++)
                {
                    _txA.Add((byte)(_channels[_selectedChannelsList[i] - 1] >> 8));
                    _txB.Add((byte)_channels[_selectedChannelsList[i] - 1]);
                }
            }
        }
        /// <summary>
        /// Подготовка данных для расчета погрешности.
        /// </summary>
        /// <param name="address">Адрес устройства.</param>
        /// <param name="selectedChannelsList">Выбранные каналы.</param>
        /// <param name="value">Значение.</param>
        public void ErrorCalculationPrepare(byte address, List<int> selectedChannelsList, double value, TA2007TestMode testMode)
        {
            short data = (short)(address << 12);

            Init();
            _testMode = testMode;
            _errorCalculationValue = value;

            _selectedChannelsList = selectedChannelsList;

            for (int i = 0; i < _selectedChannelsList.Count; i++)
            {
                _channels[_selectedChannelsList[i] - 1] = (short)(_channels[_selectedChannelsList[i] - 1] | data);
                _txA.Add((byte)(_channels[_selectedChannelsList[i] - 1] >> 8));
                _txB.Add((byte)_channels[_selectedChannelsList[i] - 1]);
            }
        }
        
        private double CountErrorCalculationResistance()
        {
            var resist = ((_results[1] - 10) / (_results[0] - 10)) * 203;

            return ((_errorCalculationValue - resist) / 203) * 100;
        }
        private double CountErrorCalculationVoltage()
        {
            var voltage = ((_results[1] - 10) / (_results[0] - 10)) * 507.5;

            return ((_errorCalculationValue - voltage) / 507.5) * 100;
        }
        private void ChannelParams_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Norm" || e.PropertyName == "Tolerance")
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(_fileName))
                    {
                        _serializer.Serialize(writer, ChannelContent);
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
    public class TA2007ChannelContent : INotifyPropertyChanged
    {
        private short _norm; // Норма.
        private short _tolerance; // Допуск.
        private short _result; // Результат.    
        private short _plusDeviation; // + Отклонение.        
        private short _minusDeviation; // - Отклонение.        
        private Brush _numberCellColor; // Цвет для ячейки с номером канала.
        private Brush _numberFontColor; // Цвет для шрифта с номером канала.
        private Brush _maxPlusDeviationFontColor; // Цвет для шрифта + отклонения.
        private Brush _maxMinusDeviationFontColor; // Цвет для шрифта - отклонения.
        private Brush[] _syncPhasColors; // Цвет для фазировки и синхронизации (Q7, Q8, Q16).

        public TA2007ChannelContent()
        {
            Result = 0;            
            PlusDeviation = 0;
            MinusDeviation = 0;
            IsError = false;

            PhasSyncColors = new Brush[] { Constants.MID_THEME_COLOR, Constants.MID_THEME_COLOR };                      
            NumberCellColor = Constants.LIGHT_THEME_COLOR;
            NumberFontColor = Constants.BODY_THEME_COLOR;
            PlusDeviationFontColor = Constants.BODY_THEME_COLOR;
            MinusDeviationFontColor = Constants.BODY_THEME_COLOR;
        }

        public int Number { get; set; }
        public short Norm
        {
            get => _norm;
            set => this.MutateVerbose(ref _norm, value, args => PropertyChanged?.Invoke(this, args));
        }
        public short Tolerance
        {
            get => _tolerance;
            set => this.MutateVerbose(ref _tolerance, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public short Result
        {
            get => _result;
            set
            {
                this.MutateVerbose(ref _result, value, args => PropertyChanged?.Invoke(this, args));
                CountDeviation();
            }
        }
        [XmlIgnore]
        public short PlusDeviation
        {
            get => _plusDeviation;
            private set => this.MutateVerbose(ref _plusDeviation, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public short MinusDeviation
        {
            get => _minusDeviation;
            private set => this.MutateVerbose(ref _minusDeviation, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush NumberCellColor
        {
            get => _numberCellColor;
            set => this.MutateVerbose(ref _numberCellColor, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush NumberFontColor
        {
            get => _numberFontColor;
            set => this.MutateVerbose(ref _numberFontColor, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush PlusDeviationFontColor
        {
            get => _maxPlusDeviationFontColor;
            private set => this.MutateVerbose(ref _maxPlusDeviationFontColor, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush MinusDeviationFontColor
        {
            get => _maxMinusDeviationFontColor;
            private set => this.MutateVerbose(ref _maxMinusDeviationFontColor, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public Brush[] PhasSyncColors
        {
            get => _syncPhasColors;
            set => this.MutateVerbose(ref _syncPhasColors, value, args => PropertyChanged?.Invoke(this, args));
        }
        [XmlIgnore]
        public bool IsError { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ResetValues()
        {
            Result = 0;
            PlusDeviation = 0;
            MinusDeviation = 0;
            IsError = false;
            PlusDeviationFontColor = Constants.BODY_THEME_COLOR;
            MinusDeviationFontColor = Constants.BODY_THEME_COLOR;
        }
        /// <summary>
        /// Получение оклонения.
        /// </summary>
        private void CountDeviation()
        {
            if (Result > Norm)
            {
                PlusDeviation = GetMaxDeviation(PlusDeviation);

                if (Result > Norm + Tolerance)
                {
                    PlusDeviationFontColor = Constants.RED_COLOR;
                    IsError = true;
                }
            }
            else if (Result < Norm)
            {
                MinusDeviation = GetMaxDeviation(MinusDeviation);

                if (Result < Norm - Tolerance)
                {
                    MinusDeviationFontColor = Constants.RED_COLOR;
                    IsError = true;
                }
            }
        }
        /// <summary>
        /// Получение максимального отклонения.
        /// </summary>
        /// <param name="currentDeviation">Текущее отклонение по конкретному каналу.</param>
        /// <returns>Максимальное отклонение по конкретному каналу.</returns>
        private short GetMaxDeviation(short currentDeviation)
        {
            short deviation = (short)(Result - Norm);

            return Math.Abs(deviation) >= Math.Abs(currentDeviation) ? deviation : currentDeviation;
        }
    }
    public class TA2007ResultText
    {
        public string Text { get; set; }
    }
}