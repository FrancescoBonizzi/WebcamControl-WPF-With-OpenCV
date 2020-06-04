using ImageProcessor;
using ImageProcessor.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

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

        public int CameraDeviceId { get; private set; }
        public byte[] LastPngFrame { get; private set; }

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
                // Creation and disposal of this object should be done in the same thread 
                // because if not it throws disconnectedContext exception
                var videoCapture = new VideoCapture();

                if (!videoCapture.Open(CameraDeviceId))
                {
                    throw new ApplicationException("Cannot connect to camera");
                }

                videoCapture.FrameWidth = _frameWidth;
                videoCapture.FrameHeight = _frameHeight;

                using (var frame = new Mat())
                {
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        videoCapture.Read(frame);

                        if (!frame.Empty())
                        {
                            // Releases the lock on first not empty frame
                            if (initializationSemaphore != null)
                                initializationSemaphore.Release();
                            _lastFrame = BitmapConverter.ToBitmap(frame);
                            var lastFrameBitmapImage = _lastFrame.ToBitmapSource();
                            lastFrameBitmapImage.Freeze();
                            _imageControlForRendering.Dispatcher.Invoke(() => _imageControlForRendering.Source = lastFrameBitmapImage);
                        }

                        // 30 FPS
                        await Task.Delay(33);
                    }
                }

                videoCapture?.Dispose();

            }, _cancellationTokenSource.Token);

            // Async initialization to have the possibility to show an animated loader without freezing the GUI
            // The alternative was the long polling. (while !variable) await Task.Delay
            await initializationSemaphore.WaitAsync();
            initializationSemaphore.Dispose();
            initializationSemaphore = null;

            if (_previewTask.IsFaulted)
                throw _previewTask.Exception;
        }

        public async Task Stop()
        {
            // If "Dispose" gets called before Stop
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            _cancellationTokenSource.Cancel();

            // Wait for it, to avoid conflicts with read/write of _lastFrame
            await _previewTask;

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
        }

    }
}
