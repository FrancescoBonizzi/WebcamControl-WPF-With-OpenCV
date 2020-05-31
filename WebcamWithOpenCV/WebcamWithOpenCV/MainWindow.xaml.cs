using System.Windows;

namespace WebcamWithOpenCV
{
    public partial class MainWindow : Window
    {
        private WebcamStreaming _webcamStreaming;

        public MainWindow()
        {
            InitializeComponent();
            cmbCameraDevices.ItemsSource = CameraDevicesEnumerator.GetAllConnectedCameras();
            cmbCameraDevices.SelectedIndex = 0;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            var selectedCameraDeviceId = cmbCameraDevices.SelectedIndex;
            if (_webcamStreaming == null || _webcamStreaming.CameraDeviceId != selectedCameraDeviceId)
            {
                _webcamStreaming?.Dispose();
                _webcamStreaming = new WebcamStreaming(
                    imageControlForRendering: webcamPreview,
                    frameWidth: 300,
                    frameHeight: 300,
                    cameraDeviceId: cmbCameraDevices.SelectedIndex);
            }

            await _webcamStreaming.Start();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Dispose();
        }
    }
}
