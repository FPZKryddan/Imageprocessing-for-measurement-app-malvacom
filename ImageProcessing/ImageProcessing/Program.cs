using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System.Drawing.Imaging;

class ImageProcessing { 
    static void Main(String[] args)
    {
        Bitmap bmp = new Bitmap(Image.FromFile(@"C:\Users\denni\source\repos\Imageprocessing-for-measurement-app-malvacom\ImageProcessing\ImageProcessing\tshirt4.jpg"));
        Mat pic = CvInvoke.Imread(@"C:\Users\denni\source\repos\Imageprocessing-for-measurement-app-malvacom\ImageProcessing\ImageProcessing\tshirt4.jpg");

        // Gaussian blur the image
        Mat gaussianBlur = new Mat();
        CvInvoke.GaussianBlur(pic, gaussianBlur, new System.Drawing.Size(3, 3), 1.0);


        // Sobel algorithm for edge detection
        Mat sobelX = new Mat();
        Mat sobelY = new Mat();
        Mat sobelXY = new Mat();

        Console.Write("HEJ");

        pic.CopyTo(sobelX);
        pic.CopyTo(sobelY);
        pic.CopyTo(sobelXY);

        CvInvoke.Sobel(gaussianBlur, sobelX, Emgu.CV.CvEnum.DepthType.Default, 1, 0, 5);
        CvInvoke.Sobel(gaussianBlur, sobelY, Emgu.CV.CvEnum.DepthType.Default, 0, 1, 5);
        CvInvoke.Sobel(gaussianBlur, sobelXY, Emgu.CV.CvEnum.DepthType.Default, 1, 1, 5);

        CvInvoke.Imshow("sobelX", sobelX);
        CvInvoke.Imshow("sobelY", sobelY);
        CvInvoke.Imshow("sobelXY", sobelXY);


        // Canny algorithm for edge detection
        Mat edgePic =  new Mat();

        var average = pic.ToImage<Gray, byte>().GetAverage();

        var lowerThreshold = Math.Max(0, (1.0 - 0.33) * average.Intensity);
        var upperThreshold = Math.Max(255, (1.0 + 0.33) * average.Intensity);

        CvInvoke.Canny(gaussianBlur, edgePic, lowerThreshold, upperThreshold, 3, true);

        CvInvoke.Imshow("canny", edgePic);

        Bitmap edgeBmp = edgePic.ToBitmap();

        // Continued work
        for (int y = 0; y < bmp.Height; y++)
        {
            for (int x = 0; x < bmp.Width; x++)
            {
                var pixel = edgeBmp.GetPixel(x,y);
                var r = pixel.R; var g = pixel.G; var b = pixel.B;
                var colorSum = r + g + b;
                if (colorSum == 0) bmp.SetPixel(x,y, Color.Transparent);
            }
        }

        bmp.Save(".\\..\\..\\..\\result.png", ImageFormat.Png);
        Console.WriteLine("Result exported");

        CvInvoke.WaitKey();
    }
}