using System.Windows;

namespace WebcamWithOpenCV
{
    public partial class MainWindow : Window
    {
        private readonly WebcamStreaming _webcamStreaming;

        public MainWindow()
        {
            InitializeComponent();
            _webcamStreaming = new WebcamStreaming(webcamPreview, 300, 300);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            _webcamStreaming.Start();
            btnStop.IsEnabled = true;
            btnStart.IsEnabled = false;
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            await _webcamStreaming.Stop();
            btnStop.IsEnabled = false;
            btnStart.IsEnabled = true;

            // To save the screenshot
            // var screnshot = _webcamStreaming.LastPngFrame;
        }

        public void Dispose()
        {
            _webcamStreaming?.Dispose();
        }
    }
}
