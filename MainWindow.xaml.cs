using OpenCvSharp.Extensions;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text;
using System.IO.Ports;
using System;

namespace FastWebCam
{
    public partial class MainWindow : Window
    {
        private const int CAMERA_RESOLUTION_WIDTH = 319;
        private const int CAMERA_RESOLUTION_HEIGHT = 240;

        private int lastCaret = 0;
        private int lastTable = 0;

        private ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();
        private CamCapturer _camCapturer;
        private SerialPortWrapper _serialPortWrapper;
        private Device _device;
        private System.Drawing.Bitmap _lastBitmap;

        public MainWindow()
        {
            InitializeComponent();

            InitializeCapturer();
            InitializeComboBox_Webcams();

            if (ComboBox_Webcams.Items.Count > 0)
            {
                ComboBox_Webcams.SelectedIndex = 0;
            }

            AssembleDevice();

            InitializeComboBox_ComPorts();
            if (ComboBox_ComPorts.Items.Count > 0)
            {
                ComboBox_ComPorts.SelectedIndex = 0;
                _device.Calibrate();
            }
        }

        private void InitializeCapturer()
        {
            _camCapturer = new CamCapturer();
            _camCapturer.OnNewFrame += camCapturer_NewFrame;
        }

        private void AssembleDevice()
        {
            _device = new Device();
            _serialPortWrapper = new SerialPortWrapper(Encoding.ASCII);

            _serialPortWrapper.OnStringReceived += s =>
            {
                DisplayCommunication(s, false);
                _device.StringReceived(s);
            };

            _device.OnStringSend += s =>
            {
                DisplayCommunication(s, true);
                _serialPortWrapper.SendString(s);
            };
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

        private void camCapturer_NewFrame(System.Drawing.Bitmap bitmap)
        {
            Dispatcher.Invoke(() =>
            {
                _lastBitmap = bitmap.Clone() as System.Drawing.Bitmap;
                Image_Frame.Source = FrameConverter.BitmapToBitmapImage(_lastBitmap);
            });
        }

        private void DisplayCommunication(string s, bool isSending)
        {
            App.Current.Dispatcher.BeginInvoke((Action)delegate
            {
                TextBox_Console.Text += s + '\n';
                Scroll.ScrollToEnd();
            });
        }

        private void ComboBox_Webcams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _camCapturer.ChangeCam(ComboBox_Webcams.SelectedIndex);
            _camCapturer.Start();
        }

        private void ComboBox_ComPorts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _serialPortWrapper.Close();
            var portName = ComboBox_ComPorts.SelectedItem.ToString();
            _serialPortWrapper.Open(portName);

            _device.DeadBeef();
        }

        private void TextBox_Input_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var s = TextBox_Input.Text + '\n';
                TextBox_Input.Text = "";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _device.Reset();
            System.Threading.Thread.Sleep(200);
            _device.StopQueueThread();
            _serialPortWrapper.Close();
            _camCapturer.Stop();
        }

        private void Rectangle_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var rectangle = sender as Canvas;
            double width = rectangle.ActualWidth;
            double height = rectangle.ActualHeight;

            var position = e.GetPosition(sender as IInputElement);

            double xFromLeftRatio = position.X / width;
            double xFromTopRatio = position.Y / height;

            _device.MoveByRatio(xFromLeftRatio, xFromTopRatio);
        }

        private void Button_PANIC_Click(object sender, RoutedEventArgs e)
        {
            _device.Reset();
        }

        private void Button_CLBRT_Click(object sender, RoutedEventArgs e)
        {
            _device.Calibrate();
        }

        private void Rectangle_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {

        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (TextBox_Input.Text.Length == 0)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Down:
                        _device.MoveByDelta(0, 1);
                        break;
                    case System.Windows.Input.Key.Up:
                        _device.MoveByDelta(0, -1);
                        break;
                    case System.Windows.Input.Key.Left:
                        _device.MoveByDelta(-1, 0);
                        break;
                    case System.Windows.Input.Key.Right:
                        _device.MoveByDelta(1, 0);
                        break;
                    default:
                        break;
                }
            }
        }

        private void Image_Frame_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var image = sender as System.Windows.Controls.Image;
            double width = image.ActualWidth;
            double height = image.ActualHeight;

            var position = e.GetPosition(sender as IInputElement);

            double deltaCaretRatio = position.X / width - 0.5;
            double deltaTableRatio = position.Y / height - 0.5;

            int deltaCaret = (int)Math.Round(deltaCaretRatio * CAMERA_RESOLUTION_WIDTH);
            int deltaTable = (int)Math.Round(deltaTableRatio * CAMERA_RESOLUTION_HEIGHT);

            _device.MoveByDelta(deltaCaret, deltaTable);
        }

        private void Button_Detect_Click(object sender, RoutedEventArgs e)
        {
            var image = _lastBitmap.ToIplImage();
            var result = Recognition.LocateRectangles(image);

            new OpenCvSharp.CvWindow(result);
        }
    }
}
