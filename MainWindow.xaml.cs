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
        private const int DEVICE_MAX_CARET = 4899;
        private const int DEVICE_MAX_TABLE = 2999;
        private const int CAMERA_RESOLUTION_WIDTH = 600;
        private const int CAMERA_RESOLUTION_HEIGHT = 400;

        private int lastCaret = 0;
        private int lastTable = 0;

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
                Blow();
                Calibrate();
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
                Scroll.ScrollToEnd();
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

        private void camCapturer_NewFrame(System.Drawing.Bitmap bitmap)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Image_Frame.Source = FrameConverter.BitmapToBitmapImage(bitmap);
            });
        }

        private void Blow()
        {
            SendCommand("\n");
        }

        private void Calibrate()
        {
            SendCommand("G28\n");
            lastCaret = 0;
            lastTable = 0;
        }

        private void Panic()
        {
            SendCommand("/X\n");
        }

        private void Move(int caret, int table)
        {
            caret = Math.Min(caret, DEVICE_MAX_CARET);
            caret = Math.Max(caret, 0);

            table = Math.Min(table, DEVICE_MAX_TABLE);
            table = Math.Max(table, 0);

            var s = String.Format("Y{0} X{1}\n", caret, table);
            SendCommand(s);
            lastCaret = caret;
            lastTable = table;
        }

        private void MoveByRatio(double caretRatio, double tableRatio)
        {
            var caret = (int)Math.Round(caretRatio * DEVICE_MAX_CARET);
            var table = (int)Math.Round(tableRatio * DEVICE_MAX_TABLE);
            Move(caret, table);
        }

        private void MoveByDelta(int deltaCaret, int deltaTable)
        {
            lastCaret += deltaCaret;
            lastTable += deltaTable;
            Move(lastCaret, lastTable);
        }

        private void SendCommand(string s)
        {
            TextBox_Console.Text += s;
            Scroll.ScrollToEnd();
            _serialPortWrapper.Send(s);
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

        private void TextBox_Input_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                var s = TextBox_Input.Text + '\n';
                SendCommand(s);
                TextBox_Input.Text = "";
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Panic();
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

            MoveByRatio(xFromLeftRatio, xFromTopRatio);
        }

        private void Button_PANIC_Click(object sender, RoutedEventArgs e)
        {
            Panic();
        }

        private void Button_CLBRT_Click(object sender, RoutedEventArgs e)
        {
            Calibrate();
        }

        private void Rectangle_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
        }

        private void Image_Frame_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var image = sender as System.Windows.Controls.Image;
            double width = image.ActualWidth;
            double height = image.ActualHeight;

            var position = e.GetPosition(sender as IInputElement);

            double deltaCaretRatio = position.X / width - 0.5;
            double deltaTableRatio = position.Y / height - 0.5;

            int deltaCaret = (int)Math.Round(deltaCaretRatio * CAMERA_RESOLUTION_WIDTH);
            int deltaTable = (int)Math.Round(deltaTableRatio * CAMERA_RESOLUTION_HEIGHT);

            MoveByDelta(deltaCaret, deltaTable);
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (TextBox_Input.Text.Length == 0)
            {
                switch (e.Key)
                {
                    case System.Windows.Input.Key.Down:
                        MoveByDelta(0, 1);
                        break;
                    case System.Windows.Input.Key.Up:
                        MoveByDelta(0, -1);
                        break;
                    case System.Windows.Input.Key.Left:
                        MoveByDelta(-1, 0);
                        break;
                    case System.Windows.Input.Key.Right:
                        MoveByDelta(1, 0);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
