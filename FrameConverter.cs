using System.IO;

namespace FastWebCam
{
    public static class FrameConverter
    {
        public static System.Windows.Media.Imaging.BitmapImage ImageToBitmapImage(System.Drawing.Image image)
        {
            System.Drawing.Image i = (System.Drawing.Bitmap)image.Clone();

            MemoryStream ms = new MemoryStream();
            i.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            System.Windows.Media.Imaging.BitmapImage bi = new System.Windows.Media.Imaging.BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            i.Dispose();

            return bi;
        }
    }
}
