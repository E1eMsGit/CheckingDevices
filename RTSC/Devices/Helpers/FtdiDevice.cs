using System;
using System.Threading;
using System.Threading.Tasks;
using FTD2XX_NET;

namespace RTSC.Devices.Helpers
{
    class FtdiDevice
    {
        private FTDI _device;
        private string _description;        
        private uint _bytesWritten;     
        private uint _bytesRead;
        private uint _bytesWrittenCounter = 0;

        public FtdiDevice(uint deviceIndex)
        {
            _device = new FTDI();
            DeviceIndex = deviceIndex;
        }

        public uint DeviceIndex { get; private set; }
        public string Description => _description;
        public bool IsDeviceOpen { get; private set; } = false;

        /// <summary>
        /// Открыть соединение с устройством.
        /// </summary>
        public void Open()
        {
            _device.OpenByIndex(DeviceIndex);
            IsDeviceOpen = _device.IsOpen;
            _device.GetDescription(out _description);
            _device.SetTimeouts(100, 100);
#if DEBUG
            Console.WriteLine($"Device[{DeviceIndex}] Open: {_device.IsOpen}");
#endif
        }

        /// <summary>
        /// Асинхронно записать массив байт в устройство.
        /// </summary>
        /// <param name="data">Данные для записи.</param>
        public async Task WriteBytesAsync(byte[] data)
        {
            await Task.Run(() => WriteBytes(data));
        }

        /// <summary>
        /// Записать массив байт в устройство.
        /// </summary>
        /// <param name="data">Данные для записи.</param>
        public void WriteBytes(byte[] data)
        {
            _device.Write(data, data.Length, ref _bytesWritten);
            _bytesWrittenCounter += _bytesWritten;
        }

        /// <summary>
        /// Асинхронно записать байт в устройство.
        /// </summary>
        /// <param name="data">Данные для записи.</param>
        public async Task WriteByteAsync(byte data)
        {
            await Task.Run(() => WriteByte(data));
        }

        /// <summary>
        /// Записать байт в устройство.
        /// </summary>
        /// <param name="data">Данные для записи.</param>
        public void WriteByte(byte data)
        {
            _device.Write(new byte[] { data }, 1, ref _bytesWritten);
            _bytesWrittenCounter += _bytesWritten;
        }

        /// <summary>
        /// Асинхронно считать массив байт из устройства.
        /// </summary>
        /// <returns>Считанный массив байт.</returns>
        public async Task<byte[]> ReadBytesAsync()
        {
            return await Task.Run(() => ReadBytes());
        }

        /// <summary>
        /// Считать массив байт из устройства.
        /// </summary>
        /// <returns>Считанный массив байт.</returns>
        public byte[] ReadBytes()
        {
            byte[] data = new byte[_bytesWrittenCounter];

            if (_bytesWrittenCounter > 0)
            {
                _device.Read(data, _bytesWrittenCounter, ref _bytesRead);
            }

            _bytesWrittenCounter = 0;

            return data;
        }
             
        /// <summary>
        /// Очистить Tx и Rx буффер устройства.
        /// </summary>
        public void Purge()
        {
            _device.Purge(FTDI.FT_PURGE.FT_PURGE_RX | FTDI.FT_PURGE.FT_PURGE_TX);
        }

        /// <summary>
        /// Закрыть соединение с устройством.
        /// </summary>
        public void Close()
        {
            _device.Close();
            IsDeviceOpen = _device.IsOpen;
#if DEBUG
            Console.WriteLine($"Device[{DeviceIndex}] Open: {_device.IsOpen}");
#endif
        }
    }
}
