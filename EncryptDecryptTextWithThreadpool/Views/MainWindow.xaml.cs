using EncryptDecryptTextWithThreadpool.ViewModels;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EncryptDecryptTextWithThreadpool.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel MainViewModel { get; set; }
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                MainViewModel = new MainViewModel(this);
                DataContext = MainViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Exception: ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        OpenFileDialog _openFile;
        CancellationTokenSource tokenSource;

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            tokenSource = new CancellationTokenSource();

            ThreadPool.QueueUserWorkItem(Start, tokenSource.Token);
        }


        private void BtnFile_Click(object sender, RoutedEventArgs e)
        {
            _openFile = new OpenFileDialog();
            _openFile.Filter = "Text documents (.txt)|*.txt";
            var result = _openFile.ShowDialog();

            if (result == true)
            {
                txbFile.Text = _openFile.FileName;
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

            Dispatcher.Invoke(new Action(() =>
            {
                filePath = txbFile.Text;
                passwordKey = txbPassword.Text;

                if (rbEncrypt.IsChecked == true)
                    isEncrypt = true;
                else if (rbDecrypt.IsChecked == true)
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
                    Dispatcher.Invoke(new Action(() => pbLoading.Value += (100 / fileText.Length)));

                    if (token.IsCancellationRequested)
                    {
                        Dispatcher.Invoke(new Action(() => pbLoading.Value = 0));
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
                    Dispatcher.Invoke(new Action(() => pbLoading.Value = 100));
                    if (isEncrypt == true)
                        MessageBox.Show("Encrypt successfully!!!");
                    else
                        MessageBox.Show("Decrypt successfully!!!");
                    Dispatcher.Invoke(new Action(() => pbLoading.Value = 0));
                }
            }

        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();
            }
        }

    }
}
