using System;
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
            cameraLoading.Visibility = Visibility.Collapsed;
        }

        private async void btnStart_Click(object sender, RoutedEventArgs e)
        {
            cameraLoading.Visibility = Visibility.Visible;
            webcamContainer.Visibility = Visibility.Collapsed;
            btnStop.IsEnabled = false;
            btnStart.IsEnabled = false;

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

            try
            {
                await _webcamStreaming.Start();
                btnStop.IsEnabled = true;
                btnStart.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                btnStop.IsEnabled = false;
                btnStart.IsEnabled = true;
            }

            cameraLoading.Visibility = Visibility.Collapsed;
            webcamContainer.Visibility = Visibility.Visible;
        }

        private async void btnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await _webcamStreaming.Stop();
                btnStop.IsEnabled = false;
                btnStart.IsEnabled = true;

                // To save the screenshot
                // var screenshot = _webcamStreaming.LastPngFrame;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
