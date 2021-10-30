using System;
using System.Windows;
using System.Windows.Media;

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
            chkQRCode.IsChecked = false;
            chkQRCode.IsEnabled = true;
            cameraLoading.Visibility = Visibility.Visible;
            webcamContainer.Visibility = Visibility.Collapsed;
            btnStop.IsEnabled = false;
            btnStart.IsEnabled = false;

            var selectedCameraDeviceId = (cmbCameraDevices.SelectedItem as CameraDevice).OpenCvId;
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
            chkQRCode.IsChecked = false;
            chkQRCode.IsEnabled = false; 

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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _webcamStreaming?.Dispose();
        }

        private void chkQRCode_Checked(object sender, RoutedEventArgs e)
        {
            if (_webcamStreaming != null)
            {
                _webcamStreaming.OnOpenCVQRCodeRead += _webcamStreaming_OnQRCodeRead;
                _webcamStreaming.OnZXingQRCodeRead += _webcamStreaming_OnZXingQRCodeRead;
            }
        }

        private void _webcamStreaming_OnZXingQRCodeRead(object sender, EventArgs e)
        {
            txtOpenZxingQRCodeData.Dispatcher.Invoke(() =>
            {
                var qrCodeData = (e as QRCodeReadEventArgs).QRCodeData;
                if (!string.IsNullOrWhiteSpace(qrCodeData))
                {
                    txtOpenZxingQRCodeData.Text = "Zxing: " + qrCodeData.SafeSubstring(0, 10) + "...";
                    txtOpenZxingQRCodeData.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    txtOpenZxingQRCodeData.Foreground = new SolidColorBrush(Colors.Red);
                }
            });
        }

        private void _webcamStreaming_OnQRCodeRead(object sender, EventArgs e)
        {
            txtOpenCVQRCodeData.Dispatcher.Invoke(() =>
            {
                var qrCodeData = (e as QRCodeReadEventArgs).QRCodeData;
                if (!string.IsNullOrWhiteSpace(qrCodeData))
                {
                    txtOpenCVQRCodeData.Text = "OpenCV: " + qrCodeData.SafeSubstring(0, 10) + "...";
                    txtOpenCVQRCodeData.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    txtOpenCVQRCodeData.Foreground = new SolidColorBrush(Colors.Red);
                }
            });
        }

        private void chkQRCode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_webcamStreaming != null)
            {
                _webcamStreaming.OnOpenCVQRCodeRead -= _webcamStreaming_OnQRCodeRead;
            }
        }
    }
}
