﻿using ImageProcessor;
using ImageProcessor.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZXing;

namespace WebcamWithOpenCV
{
    public sealed class WebcamStreaming : IDisposable
    {
        private System.Drawing.Bitmap _lastFrame;
        private Task _previewTask;

        private CancellationTokenSource _cancellationTokenSource;
        private readonly Image _imageControlForRendering;
        private readonly int _frameWidth;
        private readonly int _frameHeight;
        private readonly QRCodeDetector _qrCodeDetector;

        public int CameraDeviceId { get; private set; }
        public byte[] LastPngFrame { get; private set; }

        public event EventHandler OnOpenCVQRCodeRead;
        public event EventHandler OnZXingQRCodeRead;

        public WebcamStreaming(
            Image imageControlForRendering,
            int frameWidth,
            int frameHeight,
            int cameraDeviceId)
        {
            _imageControlForRendering = imageControlForRendering;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
            CameraDeviceId = cameraDeviceId;
            _qrCodeDetector = new QRCodeDetector();
        }

        public async Task Start()
        {
            // Never run two parallel tasks for the webcam streaming
            if (_previewTask != null && !_previewTask.IsCompleted)
                return;

            var initializationSemaphore = new SemaphoreSlim(0, 1);

            _cancellationTokenSource = new CancellationTokenSource();
            _previewTask = Task.Run(async () =>
            {
                try
                {
                    // Creation and disposal of this object should be done in the same thread 
                    // because if not it throws disconnectedContext exception
                    var videoCapture = new VideoCapture();

                    if (!videoCapture.Open(CameraDeviceId))
                    {
                        throw new ApplicationException("Cannot connect to camera");
                    }

                    using (var frame = new Mat())
                    {
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            videoCapture.Read(frame);

                            if (!frame.Empty())
                            {
                                if (OnOpenCVQRCodeRead != null)
                                {
                                    try
                                    {
                                        // Could be interesting to improve reading
                                        // https://stackoverflow.com/questions/63195577/how-to-locate-qr-code-in-large-image-to-improve-decoding-performance
                                        string qrCodeData = _qrCodeDetector.DetectAndDecode(frame, out var points);
                                        OnOpenCVQRCodeRead.Invoke(
                                            this,
                                            new QRCodeReadEventArgs(qrCodeData));

                                        for (int i = 0; i < points.Length; i++)
                                        {
                                            var point1 = points[i];
                                            var point2 = points[(i + 1) % 4];
                                            Cv2.Line(frame, (int)point1.X, (int)point1.Y, (int)point2.X, (int)point2.Y, Scalar.Green, 3);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
                                }

                                // Releases the lock on first not empty frame
                                if (initializationSemaphore != null)
                                    initializationSemaphore.Release();
                                _lastFrame = BitmapConverter.ToBitmap(frame);


                                if (OnZXingQRCodeRead != null)
                                {
                                    try
                                    {
                                        var reader = new BarcodeReader();
                                        reader.AutoRotate = true;
                                        reader.TryInverted = true;
                                        reader.Options.TryHarder = true;
                                        // detect and decode the barcode inside the bitmap
                                        var result = reader.Decode(_lastFrame);
                                        // do something with the result
                                        OnZXingQRCodeRead.Invoke(null, new QRCodeReadEventArgs(result?.Text ?? string.Empty));
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine(ex);
                                    }
                                }

                                var lastFrameBitmapImage = _lastFrame.ToBitmapSource();
                                lastFrameBitmapImage.Freeze();
                                _imageControlForRendering.Dispatcher.Invoke(
                                    () => _imageControlForRendering.Source = lastFrameBitmapImage);
                            }

                            // 30 FPS
                            await Task.Delay(33);
                        }
                    }

                    videoCapture?.Dispose();
                }
                finally
                {
                    if (initializationSemaphore != null)
                        initializationSemaphore.Release();
                }

            }, _cancellationTokenSource.Token);

            // Async initialization to have the possibility to show an animated loader without freezing the GUI
            // The alternative was the long polling. (while !variable) await Task.Delay
            await initializationSemaphore.WaitAsync();
            initializationSemaphore.Dispose();
            initializationSemaphore = null;

            if (_previewTask.IsFaulted)
            {
                // To let the exceptions exit
                await _previewTask;
            }
        }

        public async Task Stop()
        {
            // If "Dispose" gets called before Stop
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            if (!_previewTask.IsCompleted)
            {
                _cancellationTokenSource.Cancel();

                // Wait for it, to avoid conflicts with read/write of _lastFrame
                await _previewTask;
            }

            if (_lastFrame != null)
            {
                using (var imageFactory = new ImageFactory())
                using (var stream = new MemoryStream())
                {
                    imageFactory
                        .Load(_lastFrame)
                        .Resize(new ResizeLayer(
                            size: new System.Drawing.Size(_frameWidth, _frameHeight),
                            resizeMode: ResizeMode.Crop,
                            anchorPosition: AnchorPosition.Center))
                        .Save(stream);
                    LastPngFrame = stream.ToArray();
                }
            }
            else
            {
                LastPngFrame = null;
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _lastFrame?.Dispose();
            _qrCodeDetector?.Dispose();
        }

    }
}
