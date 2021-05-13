using Microsoft.Win32;
using System.IO;

namespace RTSC.Devices.Helpers
{
    public class DataFile
    {
        public string FileName { get; private set; }
        public string FilePath { get; private set; }
        public bool? DialogResult { get; private set; }
        public string FileData { get; set; }

        public void Open()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "txt files (*.txt)|*.txt"
            };

            DialogResult = openFileDialog.ShowDialog();
            
            if (DialogResult == true)
            {
                FileName = openFileDialog.SafeFileName;
                FilePath = openFileDialog.FileName;
                ReadFile();
            }
        }

        private async void ReadFile()
        {
            using (StreamReader fileStream = new StreamReader(File.OpenRead(FilePath)))
            {
                await fileStream.ReadToEndAsync()
                    .ContinueWith(t => { if (t.Result != null) FileData = t.Result.Replace("\n", "").Replace("\r", ""); });          
            }
        }
    }    
}
