using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Drawing.Imaging;
using System.Diagnostics;

class ImageProcessing {
    static void Main(String[] args)
    {
        Bitmap imageBmp = new Bitmap(Image.FromFile(@"..\..\..\tshirt4.jpg"));
        Mat imageMat = CvInvoke.Imread(@"..\..\..\tshirt4.jpg");
        Bitmap result = produceProcessedImage(ref imageMat, ref imageBmp);
        result.Save(".\\..\\..\\..\\cropresult.png", ImageFormat.Png);
    }

    static Bitmap produceProcessedImage(ref Mat imageMat, ref Bitmap imageBmp)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        // Canny algorithm for edge detection
        Mat edgeImageMat = new Mat();
        cannyAlgorithm(ref imageMat, ref edgeImageMat);

        // Capture time taken for canny algorithm
        double cannyTime = stopwatch.ElapsedMilliseconds;

        // Remove background of image
        Bitmap edgeImageBmp = edgeImageMat.ToBitmap();

        // Position of t-shirt values
        int[,] positions = new int[2, 2];
        positions[0, 0] = int.MaxValue; // min in X
        positions[0, 1] = int.MinValue; // max in X
        positions[1, 0] = int.MaxValue; // min in Y
        positions[1, 1] = int.MinValue; // max in Y

        removeBackground(ref imageBmp, ref edgeImageBmp, positions);
        double removeBackgroundTime = stopwatch.ElapsedMilliseconds - cannyTime;


        // Crop image to t-shirt boundaries
        int width = positions[0, 1] - positions[0, 0];
        int height = positions[1, 1] - positions[1, 0];
        Bitmap croppedImage = new Bitmap(width, height);
        cropImage(ref imageBmp, ref croppedImage, positions, width, height);
        double croppingTime = stopwatch.ElapsedMilliseconds - cannyTime - removeBackgroundTime;


        // Calculate waist in pixels
        float waistMeasurement = 50;
        float waistInPx = 0;
        getScale(ref croppedImage, ref waistInPx, 50);
        double getScaleTime = stopwatch.ElapsedMilliseconds - cannyTime - removeBackgroundTime - croppingTime;

        // Waist results
        float pxPerCm = waistMeasurement / waistInPx;


        // Displaying time diagnostics
        stopwatch.Stop();
        double elapsed_time = stopwatch.ElapsedMilliseconds;

        Console.WriteLine("Time to perform canny: " + cannyTime + "ms");
        Console.WriteLine("Time to remove background: " + removeBackgroundTime + "ms");
        Console.WriteLine("Time to crop image: " + croppingTime + "ms");
        Console.WriteLine("Time to get scale: " + getScaleTime + "ms");
        Console.WriteLine("Time taken in total: " + elapsed_time + "ms");

        return croppedImage;
    }

    static void cannyAlgorithm(ref Mat imageMat, ref Mat edgeImageMat)
    {
        // Gaussian blur the image
        Mat gaussianBlur = new Mat();
        CvInvoke.GaussianBlur(imageMat, gaussianBlur, new System.Drawing.Size(3, 3), 1.0);

        var average = imageMat.ToImage<Gray, byte>().GetAverage();

        var lowerThreshold = Math.Max(0, (1.0 - 0.33) * average.Intensity);
        var upperThreshold = Math.Max(255, (1.0 + 0.33) * average.Intensity);

        CvInvoke.Canny(gaussianBlur, edgeImageMat, lowerThreshold, upperThreshold, 3, true);

        CvInvoke.Imshow("canny", edgeImageMat);

    }
    static void removeBackground(ref Bitmap imageBmp, ref Bitmap edgeImageBmp, int[,] positions)
    {
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
                bool edge = isEdge(ref edgeImageBmp, x, y);

                // Getting the position of the t-shirt for cropping later
                if (edge)
                {
                    if (x < positions[0, 0]) positions[0, 0] = x;
                    else if (x > positions[0, 1]) positions[0, 1] = x;

                    if (y < positions[1, 0]) positions[1, 0] = y;
                    else if (y > positions[1, 1]) positions[1, 1] = y;
                }

                // Entering the object
                if (edge && !inObject && !columnDone)
                {
                    inObject = true;
                    distanceFromEdge = 0;
                    imageBmp.SetPixel(x, y, Color.Transparent);
                    continue;
                }
                // Exiting the object
                else if (edge && inObject && distanceFromEdge > 2)
                {
                    inObject = false;
                    Array.Clear(pixelsTBD, 0, pixelsTBD.Length);
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
    }

    // **************EXPERIMENTAL********************

    static bool findEdges(ref Bitmap edgeImageBmp, int x, int y)
    {
        bool vertical = findVerticalEdges(ref edgeImageBmp, x, y);
        bool horizontal = findHorizontalEdges(ref edgeImageBmp, x, y);
        if (!horizontal || !vertical) return false;
        return true;
    }
    
    static bool findVerticalEdges(ref Bitmap edgeImageBmp, int x, int y)
    {
        bool right = false;
        bool left = false;
        for (; x < edgeImageBmp.Width - 1; x++)
        {
            if (isEdge(ref edgeImageBmp, x, y))
            {
                right = true;
                break;
            }
        }
        for (; x > 0; x--)
        {
            if (isEdge(ref edgeImageBmp, x, y))
            {
                left = true;
                break;
            }
        }
        if (!right || !left) return false;
        return true;
    }

    static bool findHorizontalEdges(ref Bitmap edgeImageBmp, int x, int y)
    {
        bool up = false;
        bool down = false;
        for (; y < edgeImageBmp.Height - 1; y++)
        {
            if (isEdge(ref edgeImageBmp, x, y))
            {
                down = true;
                break;
            }
        }
        for (; y > 0; y--)
        {
            if (isEdge(ref edgeImageBmp, x, y))
            {
                up = true;
                break;
            }
        }
        if (!up || !down) return false;
        return true;
    }

    // **************END********************

    static bool isEdge(ref Bitmap edgeImageBmp, int x, int y)
    {
        var pixel = edgeImageBmp.GetPixel(x, y);
        var r = pixel.R; var g = pixel.G; var b = pixel.B;
        var colorSum = r + g + b;
        if (colorSum != 0) return true;
        return false;
    }

    static void cropImage(ref Bitmap srcImage, ref Bitmap dstImage, int[,] positions, int width, int height)
    {
        for (int y = positions[1, 0]; y < height + positions[1, 0]; y++)
        {
            for (int x = positions[0, 0]; x < width + positions[0, 0]; x++)
            {
                var pixel = srcImage.GetPixel(x, y);
                var r = pixel.R; var g = pixel.G; var b = pixel.B;
                var colorSum = r + g + b;

                if (colorSum != 0) dstImage.SetPixel(x - positions[0, 0], y - positions[1, 0], Color.FromArgb(pixel.ToArgb()));
            }
        }
    }

    static void getScale(ref Bitmap croppedImage, ref float waistInPx, int rayLength)
    {
        int start = 0;
        int end = 0;
        for (int x = 1; x < croppedImage.Width - 1; x++)
        {
            bool found = false;
            for (int y = croppedImage.Height - 1; y > croppedImage.Height - rayLength; y--)
            {
                var pixel = croppedImage.GetPixel(x, y);
                var a = pixel.A;

                // if non transparent pixel found
                if (a == 255)
                {
                    found = true;
                    break;
                }
            }
            // get edges of waist
            if (found && start == 0) start = x;
            else if (!found && end == 0 && start != 0)
            {
                end = x;
                break;
            }
        }
        waistInPx = end - start;
    }
}