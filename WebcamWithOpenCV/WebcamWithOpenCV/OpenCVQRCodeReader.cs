﻿using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using ZXing;
using ZXing.Common;

namespace WebcamWithOpenCV
{
    public class OpenCVQRCodeReader
    {
        private void RotateImage(Mat src, Mat dst, double angle, double scale)
        {
            var imageCenter = new Point2f(src.Cols / 2f, src.Rows / 2f);
            var rotationMat = Cv2.GetRotationMatrix2D(imageCenter, angle, scale);
            Cv2.WarpAffine(src, dst, rotationMat, src.Size());
        }

        public string DetectBarcode(Mat mat, double thresh, bool debug = false, double rotation = 0)
        {
            // load the image and convert it to grayscale
            var image = mat;

            if (rotation != 0)
            {
                RotateImage(image, image, rotation, 1);
            }

            var gray = new Mat();
            var channels = image.Channels();
            if (channels > 1)
            {
                Cv2.CvtColor(image, gray, ColorConversionCodes.BGRA2GRAY);
            }
            else
            {
                image.CopyTo(gray);
            }

            // compute the Scharr gradient magnitude representation of the images
            // in both the x and y direction
            var gradX = new Mat();
            Cv2.Sobel(gray, gradX, MatType.CV_32F, xorder: 1, yorder: 0, ksize: -1);

            var gradY = new Mat();
            Cv2.Sobel(gray, gradY, MatType.CV_32F, xorder: 0, yorder: 1, ksize: -1);

            // subtract the y-gradient from the x-gradient
            var gradient = new Mat();
            Cv2.Subtract(gradX, gradY, gradient);
            Cv2.ConvertScaleAbs(gradient, gradient);

            // blur and threshold the image
            var blurred = new Mat();
            Cv2.Blur(gradient, blurred, new Size(9, 9));

            var threshImage = new Mat();
            Cv2.Threshold(blurred, threshImage, thresh, 255, ThresholdTypes.Binary);

            // construct a closing kernel and apply it to the thresholded image
            var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(21, 7));
            var closed = new Mat();
            Cv2.MorphologyEx(threshImage, closed, MorphTypes.Close, kernel);

            // perform a series of erosions and dilations
            Cv2.Erode(closed, closed, null, iterations: 4);
            Cv2.Dilate(closed, closed, null, iterations: 4);

            //find the contours in the thresholded image, then sort the contours
            //by their area, keeping only the largest one
            Point[][] contours;
            HierarchyIndex[] hierarchyIndexes;
            Cv2.FindContours(
                closed,
                out contours,
                out hierarchyIndexes,
                mode: RetrievalModes.CComp,
                method: ContourApproximationModes.ApproxSimple);

            if (contours.Length == 0)
            {
                // throw new NotSupportedException("Couldn't find any object in the image.");
                return null;
            }

            var contourIndex = 0;
            var previousArea = 0;
            var biggestContourRect = Cv2.BoundingRect(contours[0]);
            while (contourIndex >= 0)
            {
                var contour = contours[contourIndex];

                var boundingRect = Cv2.BoundingRect(contour); //Find bounding rect for each contour
                var boundingRectArea = boundingRect.Width * boundingRect.Height;
                if (boundingRectArea > previousArea)
                {
                    biggestContourRect = boundingRect;
                    previousArea = boundingRectArea;
                }

                contourIndex = hierarchyIndexes[contourIndex].Next;
            }

            var barcode = new Mat(image, biggestContourRect); //Crop the image
            Cv2.CvtColor(barcode, barcode, ColorConversionCodes.BGRA2GRAY);

            var barcodeClone = barcode.Clone();
            var barcodeText = GetBarcodeText(barcodeClone);

            if (string.IsNullOrWhiteSpace(barcodeText))
            {
                var th = 100;
                Cv2.Threshold(barcode, barcode, th, 255, ThresholdTypes.Tozero);
                Cv2.Threshold(barcode, barcode, th, 255, ThresholdTypes.Binary);
                barcodeText = GetBarcodeText(barcode);
            }

            Cv2.Rectangle(image,
                new Point(biggestContourRect.X, biggestContourRect.Y),
                new Point(biggestContourRect.X + biggestContourRect.Width, biggestContourRect.Y + biggestContourRect.Height),
                new Scalar(0, 255, 0),
                2);

            return barcodeText;
        }

        private string GetBarcodeText(Mat barcode)
        {
            // `ZXing.Net` needs a white space around the barcode
            var barcodeWithWhiteSpace = new Mat(new Size(barcode.Width + 30, barcode.Height + 30), MatType.CV_8U, Scalar.White);
            var drawingRect = new Rect(new Point(15, 15), new Size(barcode.Width, barcode.Height));
            var roi = barcodeWithWhiteSpace[drawingRect];
            barcode.CopyTo(roi);

            return DecodeBarcodeText(barcodeWithWhiteSpace.ToBitmap());
        }

        private string DecodeBarcodeText(System.Drawing.Bitmap barcodeBitmap)
        {
            var source = new BitmapLuminanceSource(barcodeBitmap);

            var reader = new BarcodeReader(null, null, ls => new GlobalHistogramBinarizer(ls))
            {
                AutoRotate = true,
                TryInverted = true,
                Options = new DecodingOptions
                {
                    TryHarder = true
                }
            };

            var result = reader.Decode(source);
            if (result == null)
            {
                return string.Empty;
            }

            var writer = new BarcodeWriter
            {
                Format = result.BarcodeFormat,
                Options = { Width = 200, Height = 50, Margin = 4 },
                Renderer = new ZXing.Rendering.BitmapRenderer()
            };
            var barcodeImage = writer.Write(result.Text);

            return result.Text;
        }

    }
}
