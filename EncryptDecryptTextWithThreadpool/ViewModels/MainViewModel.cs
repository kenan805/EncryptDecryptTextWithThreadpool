using EncryptDecryptTextWithThreadpool.Command;
using EncryptDecryptTextWithThreadpool.Views;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace EncryptDecryptTextWithThreadpool.ViewModels
{
    public class MainViewModel
    {
        private MainWindow _mainWindow;
        private OpenFileDialog _openFile;
        private CancellationTokenSource tokenSource;

        public RelayCommand FromFileCommand { get; set; }
        public RelayCommand StartCommand { get; set; }
        public RelayCommand CancelCommand { get; set; }

        public MainViewModel(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            FromFileCommand = new RelayCommand(act => BtnFile_Click());
            StartCommand = new RelayCommand(act => BtnStart_Click());
            CancelCommand = new RelayCommand(act => BtnCancel_Click());


        }

        private void BtnStart_Click()
        {
            tokenSource = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(Start, tokenSource.Token);
        }

        private void BtnFile_Click()
        {
            _openFile = new OpenFileDialog();
            _openFile.Filter = "Text documents (.txt)|*.txt";
            var result = _openFile.ShowDialog();

            if (result == true)
            {
                _mainWindow.txbFile.Text = _openFile.FileName;
            }
        }

        public string XORCipher(string data, string key)
        {
            int dataLen = data.Length;
            int keyLen = key.Length;
            char[] output = new char[dataLen];

            for (int i = 0; i < dataLen; ++i)
            {
                output[i] = (char)(data[i] ^ key[i % keyLen]);
            }

            return new string(output);

        }

        private void Start(object state)
        {
            string filePath = "";
            string passwordKey = "";
            bool? isEncrypt = null;

            _mainWindow.Dispatcher.Invoke(new Action(() =>
            {
                filePath = _mainWindow.txbFile.Text;
                passwordKey = _mainWindow.txbPassword.Text;

                if (_mainWindow.rbEncrypt.IsChecked == true)
                    isEncrypt = true;
                else if (_mainWindow.rbDecrypt.IsChecked == true)
                    isEncrypt = false;
            }));

            EncryptOrDecrypt(filePath, passwordKey, isEncrypt, state);

        }

        private void EncryptOrDecrypt(string filePath, string passwordKey, bool? isEncrypt, object state)
        {
            bool isSuccess = true;
            var token = (CancellationToken)state;
            string[] allText;
            if (!string.IsNullOrEmpty(filePath))
            {
                var fileText = File.ReadAllLines(filePath);
                allText = fileText;
                File.WriteAllText(filePath, String.Empty);
                for (int i = 0; i < fileText.Length; i++)
                {
                    var encryptOrDecrypt = XORCipher(fileText[i], passwordKey);
                    File.AppendAllText(filePath, encryptOrDecrypt);
                    File.AppendAllText(filePath, Environment.NewLine);
                    Thread.Sleep(500);
                    _mainWindow.Dispatcher.Invoke(new Action(() => _mainWindow.pbLoading.Value += (100 / fileText.Length)));

                    if (token.IsCancellationRequested)
                    {
                        _mainWindow.Dispatcher.Invoke(new Action(() => _mainWindow.pbLoading.Value = 0));
                        if (isEncrypt == true)
                            MessageBox.Show("Encrypt cancelled!!!");
                        else
                            MessageBox.Show("Decrypt cancelled!!!");

                        isSuccess = false;
                        File.WriteAllText(filePath, String.Empty);
                        File.WriteAllLines(filePath, allText);
                        break;
                    }
                }
                if (isSuccess)
                {
                    _mainWindow.Dispatcher.Invoke(new Action(() => _mainWindow.pbLoading.Value = 100));
                    if (isEncrypt == true)
                        MessageBox.Show("Encrypt successfully!!!");
                    else
                        MessageBox.Show("Decrypt successfully!!!");
                    _mainWindow.Dispatcher.Invoke(new Action(() => _mainWindow.pbLoading.Value = 0));
                }
            }

        }

        private void BtnCancel_Click()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }
    }
}
