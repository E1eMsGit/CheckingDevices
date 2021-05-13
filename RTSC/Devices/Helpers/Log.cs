using Microsoft.Win32;
using System.IO;
using System.Text;
using System;

namespace RTSC.Devices.Helpers
{
    class Log
    {      
        private static Log _instance;
        private StringBuilder _content;

        private Log()
        {
            _content = new StringBuilder();
        }

        public static Log GetInstance() 
        {
            if (_instance == null)
            {
                _instance = new Log();
            }

            return _instance; 
        }

        public void Write(string message, bool withTime=true)
        {
            if (withTime)
            {
                _content.AppendLine($"{DateTime.Now.ToShortTimeString()}: {message}");
            }
            else
            {
                _content.AppendLine($"{message}");
            }          
        }

        public void SaveLog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "txt files (*.txt)|*.txt",
                FileName = $"Отчет {DateTime.Now.ToShortDateString()}"
            };

            
            if (saveFileDialog.ShowDialog() == true)
            {
                using (var stream = new StreamWriter(new FileStream(saveFileDialog.FileName, FileMode.Create), Encoding.UTF8))
                {
                    stream.Write(_content);
                }
            }
        }
    }
}
