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

        public async Task Start(bool convertToFakeThermalImage = false)
        {
            // Non ne faccio mai andare due in contemporanea perché si creerebbe contention sul _lastFrame
            if (_previewTask != null && !_previewTask.IsCompleted)
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            _previewTask = Task.Run(async () =>
            {
                if (_videoCapture == null)
                {
                    _videoCapture = new VideoCapture(0);
                    _videoCapture.Open(0);
                    _videoCapture.FrameWidth = _frameWidth;
                    _videoCapture.FrameHeight = _frameHeight;
                }

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

            // Per attendere l'inizializzazione della VideoCapture
            const int maxTentatives = 10;
            for (int i = 0; i < maxTentatives; ++i)
            {
                if (_videoCapture != null && _videoCapture.IsOpened())
                {
                    return;
                }
                else
                {
                    await Task.Delay(1000);
                }
            }

            if (_previewTask.IsFaulted)
                throw _previewTask.Exception;
            else
                throw new ApplicationException("Cannot connect to the Webcam");
        }

        public async Task Stop()
        {
            // Può accadere che venga chiamato prima il dispose dello stop,
            // se la pagina viene chiusa velocemente
            if (_cancellationTokenSource.IsCancellationRequested)
                return;

            _cancellationTokenSource.Cancel();

            // Aspetto che termini, così non ho conflitti con la lettura/scrittura di _lastFrame
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
