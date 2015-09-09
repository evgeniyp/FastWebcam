using AForge.Video.DirectShow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FastWebCam
{
    public class CamCapturer
    {
        private FilterInfoCollection _videoDevices;
        private VideoCaptureDevice _videoSource;

        public event Action<System.Drawing.Image> NewFrame;

        public CamCapturer()
        {
            _videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        public string[] GetCameraNames()
        {
            var result = new string[_videoDevices.Count];
            for (int i = 0; i < _videoDevices.Count; i++)
            {
                result[i] = _videoDevices[i].Name;
            }
            return result;
        }

        public void Start()
        {
            if (_videoSource != null && !_videoSource.IsRunning)
            {
                _videoSource.Start();
            }
        }

        public void Stop()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.WaitForStop();
            }
        }

        public void ChangeCam(int camNumber)
        {
            if (camNumber > -1 && _videoDevices.Count > 0 && camNumber < _videoDevices.Count)
            {
                if (_videoSource != null && _videoSource.IsRunning)
                {
                    Stop();
                }

                _videoSource = new VideoCaptureDevice(_videoDevices[camNumber].MonikerString);
                _videoSource.NewFrame += _videoSource_NewFrame;
            }
        }

        private void _videoSource_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            if (NewFrame != null)
            {
                NewFrame(eventArgs.Frame);
            }
        }
    }
}
