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
        Bitmap bmp = new Bitmap(Image.FromFile(@"..\..\..\tshirt2.jpg"));
        Mat pic = CvInvoke.Imread(@"..\..\..\tshirt2.jpg");

        // Gaussian blur the image
        Mat gaussianBlur = new Mat();
        CvInvoke.GaussianBlur(pic, gaussianBlur, new System.Drawing.Size(3, 3), 1.0);


        // Canny algorithm for edge detection
        Mat edgePic =  new Mat();

        var average = pic.ToImage<Gray, byte>().GetAverage();

        var lowerThreshold = Math.Max(0, (1.0 - 0.33) * average.Intensity);
        var upperThreshold = Math.Max(255, (1.0 + 0.33) * average.Intensity);

        CvInvoke.Canny(gaussianBlur, edgePic, lowerThreshold, upperThreshold, 3, true);

        CvInvoke.Imshow("canny", edgePic);

        Bitmap edgeBmp = edgePic.ToBitmap();

        // Fill in edges
        for (int x = 0; x < bmp.Width; x++)
        {
            bool inEdge = false;
            bool done = false;
            int edgeDistance = 0;
            int[] pixelsPTR = new int[bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
            {
                var pixel = edgeBmp.GetPixel(x,y);
                var r = pixel.R; var g = pixel.G; var b = pixel.B;
                var colorSum = r + g + b;

                if (colorSum != 0 && !inEdge && !done)
                {
                    inEdge = true;
                    edgeDistance = 0;
                    bmp.SetPixel(x, y, Color.Transparent);
                    continue;
                }
                else if (colorSum != 0 && inEdge && edgeDistance > 2)
                {
                    inEdge = false;
                    for (int i = 0; i < pixelsPTR.Length; i++) pixelsPTR[i] = 0;
                    done = true;
                }

                if (!inEdge) bmp.SetPixel(x, y, Color.Transparent);
                else
                {
                    edgeDistance++;
                    pixelsPTR[y] = 1;
                }
            }

            if (inEdge && edgeDistance > 2)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    if (pixelsPTR[y] == 1)
                    {
                        bmp.SetPixel(x, y, Color.Transparent);
                    }
                }
            }
        }




        CvInvoke.Imwrite("edge.png", edgePic);

        bmp.Save(".\\..\\..\\..\\result.png", ImageFormat.Png);
        Console.WriteLine("Result exported");

        CvInvoke.WaitKey();
    }
}