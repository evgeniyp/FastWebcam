using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading;

namespace FastWebCam
{
    public partial class MainWindow : Window
    {
        private ImageSourceConverter _imageSourceConverter = new ImageSourceConverter();
        private CamCapturer _camCapturer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCapturer();
            InitializeComboBox_Webcams();

            if (ComboBox_Webcams.Items.Count > 0)
            {
                ComboBox_Webcams.SelectedIndex = 0;
            }
        }

        private void InitializeCapturer()
        {
            _camCapturer = new CamCapturer();
            _camCapturer.NewFrame += camCapturer_NewFrame;
        }

        private void InitializeComboBox_Webcams()
        {
            var cameraNames = _camCapturer.GetCameraNames();
            foreach (var item in cameraNames)
            {
                ComboBox_Webcams.Items.Add(item);
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _camCapturer.Stop();
        }
    }
}
