using ImageProcessor;
using ImageProcessor.Imaging;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.IO;
using System.Linq;
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
        private VideoCapture _videoCapture;
        private readonly Image _imageControlForRendering;
        private readonly int _frameWidth;
        private readonly int _frameHeight;

        public byte[] LastPngFrame { get; private set; }

        public WebcamStreaming(
            Image imageControlForRendering,
            int frameWidth,
            int frameHeight)
        {
            _imageControlForRendering = imageControlForRendering;
            _frameWidth = frameWidth;
            _frameHeight = frameHeight;
        }

        public void Start()
        {
            // Never run two parallel tasks for the webcam streaming
            if (_previewTask != null && !_previewTask.IsCompleted)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            if (_videoCapture == null)
            {
                _videoCapture = new VideoCapture(0);
                if (!_videoCapture.Open(0))
                {
                    throw new ApplicationException("Cannot connect to the Webcam");
                }

                _videoCapture.FrameWidth = _frameWidth;
                _videoCapture.FrameHeight = _frameHeight;
            }

            _previewTask = Task.Run(async () =>
                {
                    using (var frame = new Mat())
                    {
                        while (!_cancellationTokenSource.IsCancellationRequested)
                        {
                            _videoCapture.Read(frame);

                            if (!frame.Empty())
                            {
                                _lastFrame = BitmapConverter.ToBitmap(frame);
                                var lastFrameBitmapImage = _lastFrame.ToBitmapSource();
                                lastFrameBitmapImage.Freeze();

                                _imageControlForRendering.Dispatcher.Invoke(() => _imageControlForRendering.Source = lastFrameBitmapImage);
                                await Task.Delay(10);
                            }
                        }
                    }
                }, _cancellationTokenSource.Token);

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
            if (_videoCapture?.IsDisposed == false)
            {
                _videoCapture?.Dispose();
                _videoCapture = null;
            }
            _lastFrame?.Dispose();
        }
    }
}
