using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.Threading;
using System.IO.Ports;
using System;

namespace FastWebCam
{
    public partial class MainWindow : Window
    {
        private ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();
        private CamCapturer _camCapturer;
        private SerialPortWrapper _serialPortWrapper;

        public MainWindow()
        {
            InitializeComponent();

            InitializeCapturer();
            InitializeComboBox_Webcams();

            if (ComboBox_Webcams.Items.Count > 0)
            {
                ComboBox_Webcams.SelectedIndex = 0;
            }

            InitializeSerialPortWrapper();

            InitializeComboBox_ComPorts();
            if (ComboBox_ComPorts.Items.Count > 0)
            {
                ComboBox_ComPorts.SelectedIndex = 0;
            }
        }

        private void InitializeCapturer()
        {
            _camCapturer = new CamCapturer();
            _camCapturer.OnNewFrame += camCapturer_NewFrame;
        }

        private void InitializeSerialPortWrapper()
        {
            _serialPortWrapper = new SerialPortWrapper(Encoding.ASCII);
            _serialPortWrapper.OnData += _serialPortWrapper_OnData;
            _serialPortWrapper.OnException += _serialPortWrapper_OnException;
        }

        private void _serialPortWrapper_OnException(Exception e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                TextBox_Console.Text += "Ошибка: " + e.Message;
            });
        }

        private void _serialPortWrapper_OnData(string s)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                TextBox_Console.Text += s + '\n';
            });
        }

        private void InitializeComboBox_Webcams()
        {
            var cameraNames = _camCapturer.GetCameraNames();
            foreach (var item in cameraNames)
            {
                ComboBox_Webcams.Items.Add(item);
            }
        }

        private void InitializeComboBox_ComPorts()
        {
            ComboBox_ComPorts.Items.Clear();
            var portNames = SerialPort.GetPortNames();
            foreach (var portName in portNames)
            {
                ComboBox_ComPorts.Items.Add(portName);
            }
            if (ComboBox_ComPorts.Items.Count > 0)
            {
                ComboBox_ComPorts.SelectedIndex = 0;
            }
        }

        private void camCapturer_NewFrame(System.Drawing.Image frame)
        {
            var source = FrameConverter.ImageToBitmapImage(frame);

            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                Image_Frame.Source = source;
            }));
        }

        private void ComboBox_Webcams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _camCapturer.ChangeCam(ComboBox_Webcams.SelectedIndex);
            _camCapturer.Start();
        }

        private void ComboBox_ComPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serialPortWrapper.Close();
            _serialPortWrapper.Open(ComboBox_ComPorts.SelectedItem.ToString());
        }

        private void TextBox_Input_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var s = TextBox_Input.Text + '\n';
                _serialPortWrapper.Send(s);
                TextBox_Input.Text = "";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _serialPortWrapper.Close();
            _camCapturer.Stop();
        }
    }
}
