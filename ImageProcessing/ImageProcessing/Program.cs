﻿using System;
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
using System.Diagnostics;

class ImageProcessing { 
    static void Main(String[] args)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start(); 

        Bitmap imageBmp = new Bitmap(Image.FromFile(@"..\..\..\tshirt4.jpg"));
        Mat imageMat = CvInvoke.Imread(@"..\..\..\tshirt4.jpg");

        // Gaussian blur the image
        Mat gaussianBlur = new Mat();
        CvInvoke.GaussianBlur(imageMat, gaussianBlur, new System.Drawing.Size(3, 3), 1.0);


        // Canny algorithm for edge detection
        Mat edgeImageMat =  new Mat();

        var average = imageMat.ToImage<Gray, byte>().GetAverage();

        var lowerThreshold = Math.Max(0, (1.0 - 0.33) * average.Intensity);
        var upperThreshold = Math.Max(255, (1.0 + 0.33) * average.Intensity);

        CvInvoke.Canny(gaussianBlur, edgeImageMat, lowerThreshold, upperThreshold, 3, true);

        CvInvoke.Imshow("canny", edgeImageMat);

        // Capture time taken for canny algorithm
        double cannyTime = stopwatch.ElapsedMilliseconds;

        // Remove background of image
        Bitmap edgeimageBmp = edgeImageMat.ToBitmap();

        // Position of t-shirt values
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        // Fill in edges
        for (int x = 0; x < imageBmp.Width; x++)
        {
            bool inObject = false;
            bool columnDone = false;
            int distanceFromEdge = 0;
            int[] pixelsTBD = new int[imageBmp.Height]; // Pixels that might need to get deleted

            for (int y = 0; y < imageBmp.Height; y++)
            {
                // get current pixel values
                var pixel = edgeimageBmp.GetPixel(x,y);
                var r = pixel.R; var g = pixel.G; var b = pixel.B;
                var colorSum = r + g + b;

                // Getting the position of the t-shirt for cropping later
                if (colorSum != 0)
                {
                    if (x < minX) minX = x;
                    else if (x > maxX) maxX = x;

                    if (y < minY) minY = y;
                    else if (y > maxY) maxY = y;
                }

                // Entering the object
                if (colorSum != 0 && !inObject && !columnDone)
                {
                    inObject = true;
                    distanceFromEdge = 0;
                    imageBmp.SetPixel(x, y, Color.Transparent);
                    continue;
                } 
                // Exiting the object
                else if (colorSum != 0 && inObject && distanceFromEdge > 2)
                {
                    inObject = false;
                    for (int i = 0; i < pixelsTBD.Length; i++) pixelsTBD[i] = 0;
                    columnDone = true;
                }

                // if we are outside the object then turn the pixel transparent, otherwise increase distance from edge and add pixels to pixelsTBD.
                if (!inObject) imageBmp.SetPixel(x, y, Color.Transparent);
                else
                {
                    distanceFromEdge++;
                    pixelsTBD[y] = 1;
                }
            }

            // If we have pixels in pixelsTBD and we never found a exiting edge in this column then remove all pixels in pixelsTBD.
            if (inObject && distanceFromEdge > 2)
            {
                for (int y = 0; y < imageBmp.Height; y++)
                    if (pixelsTBD[y] == 1) imageBmp.SetPixel(x, y, Color.Transparent);
            }
        }

        // Capture time taken for removing background
        double removeBackgroundTime = stopwatch.ElapsedMilliseconds - cannyTime;

        // Crop image to t-shirt boundaries
        int width = maxX - minX;
        int height = maxY - minY;
        Bitmap croppedImage = new Bitmap(width, height);
        for (int y = minY; y < height + minY; y++)
        {
            for (int x = minX; x < width + minX; x++)
            {
                var pixel = imageBmp.GetPixel(x, y);
                var r = pixel.R; var g = pixel.G; var b = pixel.B;
                var colorSum = r + g + b;

                if (colorSum != 0) croppedImage.SetPixel(x - minX, y - minY, Color.FromArgb(pixel.ToArgb()));
            }
        }

        // Capture time taken for cropping the image
        double croppingTime = stopwatch.ElapsedMilliseconds - cannyTime - removeBackgroundTime;

        // Export results
        imageBmp.Save(".\\..\\..\\..\\result.png", ImageFormat.Png);
        Console.WriteLine("Removed Background Result exported");

        croppedImage.Save(".\\..\\..\\..\\cropresult.png", ImageFormat.Png);
        Console.WriteLine("Cropped Result exported");

        // Displaying time diagnostics
        stopwatch.Stop();
        double elapsed_time = stopwatch.ElapsedMilliseconds;
        Console.WriteLine("Time to perform canny: " + cannyTime + "ms");
        Console.WriteLine("Time to remove background: " + removeBackgroundTime + "ms");
        Console.WriteLine("Time to crop image: " + croppingTime + "ms");
        Console.WriteLine("Time taken in total: " + elapsed_time + "ms");

        CvInvoke.WaitKey();
    }
}