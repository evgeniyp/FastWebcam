using OpenCvSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FastWebCam
{
    public class CvBox2D_Comparer_By_Area : IComparer
    {
        // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
        int IComparer.Compare(Object aA, Object aB)
        {
            if (aA is CvBox2D && aB is CvBox2D)
            {
                CvBox2D __a = (CvBox2D)aA, __b = (CvBox2D)aB;
                return (int)(Math.Round(__b.Size.Height * __b.Size.Width) - Math.Round(__a.Size.Height * __a.Size.Width));
            }
            else return 0;
        }
    }

    public static class Recognition
    {
        private static double RadToDegree(double aRad)
        {
            return aRad / Math.PI * 180.0;
        }

        // !!!!!!!!!!!!!!!!Требуется отладка - похоже функция работает неправильно!
        // Функция поиска угла прямой, заданной двумя точками P1 и P2 
        public static double GetAngle(CvPoint P1, CvPoint P2)
        {
            // считаем центральной точку находящуюся выше,
            CvPoint aCurr, aPrev, aNext;
            aCurr = (P1.Y < P2.Y) ? P1 : P2;
            aPrev = (P1.Y < P2.Y) ? P2 : P1;

            aNext.X = aCurr.X;
            aNext.Y = 1000;
            if (aCurr.X > aPrev.X) return -GetAngle(aCurr, aPrev, aNext);
            else return GetAngle(aCurr, aPrev, aNext);
        }

        // Функция поиска угла между прямыми, заданными точками (aCurrent aPrevious) и (aCurrent aNext)
        public static double GetAngle(CvPoint aCurr, CvPoint aPrev, CvPoint aNext)
        {
            // проверяем на нулевую длинну
            double a = CvPoint.Distance(aCurr, aNext);
            double b = CvPoint.Distance(aCurr, aPrev);
            double c = CvPoint.Distance(aPrev, aNext);
            if (a == 0 || b == 0 || c == 0) return 0.0;
            double _angle = Math.Acos((a * a + b * b - c * c) / (2 * a * b));
            return RadToDegree(_angle);
        }

        public static double BoxAngle(CvPoint2D32f[] pt)
        {
            if (pt.Length < 4) return Double.NaN;

            // ищем самую высокую точку
            double _min_Y = double.MaxValue;
            int _ind = -1;

            for (int i = 0; i < pt.Length; i++)
            {
                if (pt[i].Y <= _min_Y)
                {
                    _min_Y = pt[i].Y;
                    _ind = i;
                }
            }

            //Ищем самую длинную ось от этой точки
            CvPoint p; // current point
            CvPoint p_prev; //previous point
            CvPoint p_next; //next point
            CvPoint p_angle;

            p = pt[_ind];
            p_prev = (_ind == 0) ? pt[3] : pt[_ind - 1];
            p_next = (_ind == 3) ? pt[0] : pt[_ind + 1];

            double a = CvPoint.Distance(p, p_prev);
            double b = CvPoint.Distance(p, p_next);
            double _angle;

            p_angle = (a > b) ? p_prev : p_next;

            if (p_angle.X > p.X)
            {
                _angle = -GetAngle(p_angle, p, new CvPoint(p_angle.X, p.Y));
            }
            else
            {
                _angle = GetAngle(p_angle, p, new CvPoint(p_angle.X, p.Y));
            }
            return _angle;
        }

        public static IplImage MakeBorder(IplImage src, BorderType bordertype, CvScalar value) //, FABorderType btype)
        {
            IplImage dst = src.Clone();

            int channels = dst.NChannels;
            IntPtr ptr = dst.ImageData;
            int _border = 2;

            // рисуем инверсную рамку 
            for (int x = 0; x < dst.Width; x++)
            {
                for (int y = 0; y < dst.Height; y++)
                    if ((x < _border || x > dst.Width - _border - 1) || (y < _border || y > dst.Height - _border - 1))
                    {
                        int offset = (dst.WidthStep * y) + (x * channels);
                        if (channels == 1)
                        {
                            byte g = Marshal.ReadByte(ptr, offset); // Grayscale
                            if (bordertype == BorderType.Constant)
                            {
                                g = (byte)value.Val0;
                            }
                            else if (bordertype == BorderType.Reflict)
                            {
                                g = (g < 50 || g > 200) ? (byte)(255 - g) : (byte)0;
                            }

                            Marshal.WriteByte(ptr, offset, g);  //G
                        }
                        else if (channels == 3)
                        {
                            byte b = Marshal.ReadByte(ptr, offset + 0);    // B
                            byte g = Marshal.ReadByte(ptr, offset + 1);    // G
                            byte r = Marshal.ReadByte(ptr, offset + 2);    // R

                            if (bordertype == BorderType.Constant)
                            {
                                r = (byte)value.Val0;
                                g = (byte)value.Val1;
                                b = (byte)value.Val2;

                            }
                            else if (bordertype == BorderType.Reflict)
                            {

                                b = (bordertype == BorderType.Constant) ? (byte)value : (b < 50 || b > 200) ? (byte)(255 - b) : (byte)0;
                                g = (bordertype == BorderType.Constant) ? (byte)value : (g < 50 || g > 200) ? (byte)(255 - g) : (byte)0;
                                r = (bordertype == BorderType.Constant) ? (byte)value : (r < 50 || r > 200) ? (byte)(255 - r) : (byte)0;
                            }

                            Marshal.WriteByte(ptr, offset + 0, b);  //B
                            Marshal.WriteByte(ptr, offset + 1, g);  //G
                            Marshal.WriteByte(ptr, offset + 2, r);  //R
                        }
                    }
            }

            return dst;
        }

        public static IplImage MakeBorder(IplImage src)
        {
            return MakeBorder(src, BorderType.Reflict, new CvScalar(0));
        }

        public static IplImage LocateRectangles(IplImage src)
        {
            // Prepare source Image 
            // Convert to GrayScale
            IplImage __gray = Cv.CreateImage(src.Size, src.Depth, 1);
            if (src.NChannels > 1)
                Cv.CvtColor(src, __gray, ColorConversion.RgbToGray);
            else __gray = src.Clone();

            //Smooth
            IplImage __gray_src = __gray.Clone();
            Cv.Smooth(__gray, __gray, SmoothType.Gaussian, 9);

            //MakeBorder
            __gray = MakeBorder(__gray);

            // Canny
            double threshold1 = 10;
            double threshold2 = 50;

            Cv.Canny(__gray, __gray, threshold1, threshold2, ApertureSize.Size3);
            // утолщаем линии (это дает нам внутренние контуры, которые и содержат важную информацию)
            Cv.Dilate(__gray, __gray);

            IplImage dst = Cv.CreateImage(src.Size, src.Depth, 3);
            if (src.NChannels == 1)
                Cv.CvtColor(src, dst, ColorConversion.GrayToRgb);
            else dst = src.Clone();

            // Тут будем хранить найденные прямоугольники
            ArrayList rectangles = new ArrayList();


            //Собственно разбираем кадр и ищем прямоугольные объекты
            using (CvMemStorage storage = new CvMemStorage(0))
            {
                CvSeq<CvPoint> contours = null;

                int contoursCount = Cv.FindContours(__gray, storage, out contours, CvContour.SizeOf, ContourRetrieval.List, ContourChain.ApproxSimple, Cv.Point(0, 0));

                //освобождаем память
                __gray.Dispose();

                contours = Cv.ApproxPoly(contours, CvContour.SizeOf, storage, ApproxPolyMethod.DP, 5, true);


                double _angle;

                for (CvSeq<CvPoint> seq0 = contours; seq0 != null; seq0 = seq0.HNext)
                {

                    double _area = Math.Abs(seq0.ContourArea(CvSlice.WholeSeq)); // площадь контура
                    double _perim = seq0.ContourPerimeter();     // периметр контура

                    // нас интересует  MinAreaRect2
                    CvBox2D box = Cv.MinAreaRect2(seq0);
                    CvPoint2D32f[] pt;
                    Cv.BoxPoints(box, out pt);

                    // не рассматриваем объекты, площадь которых больше 60% кадра
                    double _img_area = (double)(dst.Height * dst.Width) * 0.6;

                    double _box_area = box.Size.Height * box.Size.Width;

                    // Выкидываем прямоугольники маленького размера
                    int _min_area = 320;
                    int _min_box_area = 60;

                    bool conditions = (_area > _min_area && _box_area > _min_box_area && _area < _img_area);

                    // объект похож на прямоугольник нужного размера?
                    if (conditions)
                    {
                        double _box_angle = box.Angle;
                        _angle = BoxAngle(pt);
                        rectangles.Add(box);

                        if (false)
                            using (IplImage _gray = src.Clone())
                            {
                                Cv.BoxPoints(box, out pt);
                                Cv.Line(_gray, pt[0], pt[1], CvColor.Red, 3);
                                Cv.Line(_gray, pt[1], pt[2], CvColor.Red, 3);
                                Cv.Line(_gray, pt[2], pt[3], CvColor.Red, 3);
                                Cv.Line(_gray, pt[3], pt[0], CvColor.Red, 3);

                                Cv.NamedWindow("Box", WindowMode.AutoSize);
                                Cv.ShowImage("Box", _gray);

                                Cv.WaitKey(0);
                                Cv.DestroyAllWindows();
                            }

                    }
                }
            }

            // Убираем все внутренние прямоугольники - для этого сортируем их по размеру и удаляем все прямоуогльники, центр которых находится внутри бОльших
            if (rectangles.Count == 0) return dst;

            // Сортируем по размеру - бОльшие расположены выше
            rectangles.Sort(new CvBox2D_Comparer_By_Area());

            CvPoint2D32f[] p;

            // удаляем все внутренние
            for (int i = rectangles.Count - 1; i > 0; i--)
                for (int j = i - 1; j >= 0; j--)
                {
                    Cv.BoxPoints(((CvBox2D)rectangles[j]), out p);

                    if (false)
                        using (IplImage _gray = src.Clone())
                        {

                            Cv.Line(_gray, p[0], p[1], CvColor.Red, 3);
                            Cv.Line(_gray, p[1], p[2], CvColor.Red, 3);
                            Cv.Line(_gray, p[2], p[3], CvColor.Red, 3);
                            Cv.Line(_gray, p[3], p[0], CvColor.Red, 3);

                            CvPoint2D32f[] p2;
                            Cv.BoxPoints(((CvBox2D)rectangles[i]), out p2);
                            Cv.Circle(_gray, ((CvBox2D)rectangles[i]).Center, 5, CvColor.Blue, 2, LineType.AntiAlias, 0);
                            Cv.Line(_gray, p2[0], p2[1], CvColor.Blue, 3);
                            Cv.Line(_gray, p2[1], p2[2], CvColor.Blue, 3);
                            Cv.Line(_gray, p2[2], p2[3], CvColor.Blue, 3);
                            Cv.Line(_gray, p2[3], p2[0], CvColor.Blue, 3);


                            Cv.NamedWindow("Box", WindowMode.AutoSize);
                            Cv.ShowImage("Box", _gray);

                            Cv.WaitKey(0);
                            Cv.DestroyAllWindows();
                        }


                    if (point_is_inside_rect(((CvBox2D)rectangles[i]).Center, p))
                    {
                        rectangles.RemoveAt(i);
                        break;
                    }
                }


            // подсвечиваем все найденные прямоугольники
            for (int i = 0; i < rectangles.Count; i++)
            {
                CvBox2D b = (CvBox2D)rectangles[i];
                Cv.BoxPoints(b, out p);
                Cv.Line(dst, p[0], p[1], CvColor.Red, 3);
                Cv.Line(dst, p[1], p[2], CvColor.Red, 3);
                Cv.Line(dst, p[2], p[3], CvColor.Red, 3);
                Cv.Line(dst, p[3], p[0], CvColor.Red, 3);

            }

            //освобождаем память
            __gray_src.Dispose();

            return dst;
        }

        public static bool point_is_inside_rect(CvPoint2D32f p, CvPoint2D32f[] b)
        {
            float ax = b[0].X - p.X;
            float ay = b[0].Y - p.Y;
            float bx = b[1].X - p.X;
            float by = b[1].Y - p.Y;
            float cx = b[2].X - p.X;
            float cy = b[2].Y - p.Y;
            float dx = b[3].X - p.X;
            float dy = b[3].Y - p.Y;

            float ab = ax * by - ay * bx;
            float bc = bx * cy - by * cx;
            float cd = cx * dy - cy * dx;
            float da = dx * ay - dy * ax;

            if ((ab >= 0 && bc >= 0 && cd >= 0 && da >= 0) || (ab <= 0 && bc <= 0 && cd <= 0 && da <= 0)) return true;
            else return false;
        }
    }
}
