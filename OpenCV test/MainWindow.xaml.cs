using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.WPF;
using Microsoft.Win32;
using System;
using System.Windows;
using Emgu.CV.Features2D;
using System.Collections.Generic;

namespace OpenCV_test
{
    /// <summary>
    /// Тестовое задание.
    /// К письму приложены черно-белые изображения, на которых видно руку.
    /// Нужно написать программу, которая бы принимала на вход изображение и отмечала на нем красной точкой кончики пальцев
    /// и обводила ладонь синим контуром.
    /// </summary>
    public partial class MainWindow : Window
    {
        private Image<Bgr, byte> InputImage = null;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog OpenImageDialog = new OpenFileDialog();
                OpenImageDialog.Filter = "Картинки (*.jpg; *.bmp; *.jpeg; *.png)| *.jpg; *.bmp; *.jpeg; *.png";


                if (OpenImageDialog.ShowDialog() == true)
                {
                    InputImage = new Image<Bgr, byte>(OpenImageDialog.FileName);
                    InputImageBox.Source = BitmapSourceConvert.ToBitmapSource(InputImage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImageRecognition_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Image<Gray, byte> OutputImage = InputImage.Convert<Gray, byte>().ThresholdBinary(new Gray(100), new Gray(255));
                int i;

                //Применение фильтрации при необходимости
                if (FilterBox.IsChecked == true)
                {
                    CvInvoke.Erode(OutputImage, OutputImage, new Mat(), new System.Drawing.Point(-1, -1), 5, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
                    CvInvoke.Dilate(OutputImage, OutputImage, new Mat(), new System.Drawing.Point(-1, -1), 5, Emgu.CV.CvEnum.BorderType.Default, new MCvScalar());
                }

                //Поиск контура
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                Mat hierarchy = new Mat();
                CvInvoke.FindContours(OutputImage,
                    contours,
                    hierarchy,
                    Emgu.CV.CvEnum.RetrType.Tree,
                    Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                //ResultImage для фильтров
                Image<Bgr, byte> ResultImage = OutputImage.Convert<Bgr, byte>();

                //Отрисовка контура
                CvInvoke.DrawContours(ResultImage, contours, -1, new MCvScalar(255, 10, 10), 3);

                //Опорные точки
                VectorOfPoint contour = new VectorOfPoint();
                VectorOfInt hull = new VectorOfInt();
                contour = contours[0];
                CvInvoke.ConvexHull(contour, hull, false, false);
                
                //Дефекты выпуклости
                Mat convexityDefects = new Mat();
                CvInvoke.ConvexityDefects(contour, hull, convexityDefects);

                //Преобразование в матрицу для удобства
                Matrix<int> matrixOfDefects = new Matrix<int>(convexityDefects.Rows, convexityDefects.Cols, convexityDefects.NumberOfChannels);
                convexityDefects.CopyTo(matrixOfDefects);
                Matrix<int>[] channels = matrixOfDefects.Split();
                CircleF circle = CvInvoke.MinEnclosingCircle(contour);
                //
                List<System.Drawing.PointF> PointsMemory = new List<System.Drawing.PointF>();

                //Для поиска центра
                RotatedRect minAreaRect = CvInvoke.MinAreaRect(contour);

                //Отбор точек
                for (i = 0; i < matrixOfDefects.Rows; ++i)
                {
                    if (PropertyBox.IsChecked == true)
                    {
                    #region Через Описывающий прямоугольник
                    //C - Center  S - Start[0]  E - End[1]  D - Depth[2]
                    //Start - Center
                    float LengthXSC = contour[channels[0][i, 0]].X - minAreaRect.Center.X;
                    float LengthYSC = contour[channels[0][i, 0]].Y - minAreaRect.Center.Y;
                    float LengthSC = (float)Math.Sqrt(Math.Pow(LengthXSC, 2) + Math.Pow(LengthYSC, 2));

                    //Расстояние от начальной точки до дефекта
                    float LengthXSD = contour[channels[0][i, 0]].X - contour[channels[2][i, 0]].X;
                    float LengthYSD = contour[channels[0][i, 0]].Y - contour[channels[2][i, 0]].X;
                    float LengthSD = (float)Math.Sqrt(Math.Pow(LengthXSD, 2) + Math.Pow(LengthYSD, 2));

                    //Расстояние от начала до конца вектора
                    float LengthXSE = contour[channels[0][i, 0]].X - contour[channels[1][i, 0]].X;
                    float LengthYSE = contour[channels[0][i, 0]].Y - contour[channels[1][i, 0]].X;
                    float LengthSE = (float)Math.Sqrt(Math.Pow(LengthXSE, 2) + Math.Pow(LengthYSE, 2));

                    //Расстояние от дефекта до центра
                    float LengthXDC = contour[channels[2][i, 0]].X - minAreaRect.Center.X;
                    float LengthYDC = contour[channels[2][i, 0]].Y - minAreaRect.Center.Y;
                    float LengthDC = (float)Math.Sqrt(Math.Pow(LengthXDC, 2) + Math.Pow(LengthYDC, 2));

                    //Расстояние от центра до грани под 90
                    float MinRadius = minAreaRect.Size.Width / 2;

                    //Отбор точек по условиям
                    if (LengthSC >= MinRadius * 0.85 &&
                       (LengthSE <= MinRadius || LengthSE >= MinRadius * 0.95) &&
                       (LengthSD > MinRadius * 0.3 && LengthDC <= MinRadius * 0.9))
                    {
                        PointsMemory.Add(new System.Drawing.PointF(contour[channels[0][i, 0]].X, contour[channels[0][i, 0]].Y));
                        ResultImage.Draw(new System.Drawing.Point[] { contour[channels[0][i, 0]], contour[channels[2][i, 0]] }, new Bgr(0, 255, 0), 2);                  
                    }
                        ResultImage.Draw(new RotatedRect(minAreaRect.Center, minAreaRect.Size, minAreaRect.Angle), new Bgr(125, 125, 125), 2);
                        #endregion
                    }
                    else
                    {
                    #region Через описывающую окружность
                    //C - Center  S - Start[0]  E - End[1]  D - Depth[2]
                    //Start - Center
                    float LengthXSC = contour[channels[0][i, 0]].X - circle.Center.X;
                    float LengthYSC = contour[channels[0][i, 0]].Y - circle.Center.Y;
                    float LengthSC = (float)Math.Sqrt(Math.Pow(LengthXSC, 2) + Math.Pow(LengthYSC, 2));

                    //Расстояние от начальной точки до дефекта
                    float LengthXSD = contour[channels[0][i, 0]].X - contour[channels[2][i, 0]].X;
                    float LengthYSD = contour[channels[0][i, 0]].Y - contour[channels[2][i, 0]].X;
                    float LengthSD = (float)Math.Sqrt(Math.Pow(LengthXSD, 2) + Math.Pow(LengthYSD, 2));

                    //Расстояние от начала до конца вектора
                    float LengthXSE = contour[channels[0][i, 0]].X - contour[channels[1][i, 0]].X;
                    float LengthYSE = contour[channels[0][i, 0]].Y - contour[channels[1][i, 0]].X;
                    float LengthSE = (float)Math.Sqrt(Math.Pow(LengthXSE, 2) + Math.Pow(LengthYSE, 2));

                    //Расстояние от дефекта до центра
                    float LengthXDC = contour[channels[2][i, 0]].X - circle.Center.X;
                    float LengthYDC = contour[channels[2][i, 0]].Y - circle.Center.Y;
                    float LengthDC = (float)Math.Sqrt(Math.Pow(LengthXDC, 2) + Math.Pow(LengthYDC, 2));


                    //Отбор точек по условиям
                    if (LengthSC >= circle.Radius * 0.5 &&
                       (LengthSE <= circle.Radius * 0.8 || LengthSE >= circle.Radius)&&
                        (LengthDC <= LengthSD*0.9 || LengthSD > circle.Radius*0.4))
                    {
                        ResultImage.Draw(new System.Drawing.Point[] { contour[channels[0][i, 0]], contour[channels[2][i, 0]] }, new Bgr(0, 255, 0), 2);
                        PointsMemory.Add(new System.Drawing.PointF(contour[channels[0][i, 0]].X, contour[channels[0][i, 0]].Y));
                    }
                    ResultImage.Draw(new CircleF(circle.Center, circle.Radius), new Bgr(0, 255, 255), 2);
                    #endregion
                    }                    
                }
                foreach (System.Drawing.PointF pt in PointsMemory)
                {
                    ResultImage.Draw(new CircleF(pt, 3), new Bgr(0, 0, 255), 3);                  
                }
                           
                OutputImageBox.Source = BitmapSourceConvert.ToBitmapSource(ResultImage);
                

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }        
    }
}
