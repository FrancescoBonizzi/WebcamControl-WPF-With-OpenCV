using System;
using System.Windows;
using System.Windows.Documents;
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
                _webcamStreaming.OnQRCodeRead += _webcamStreaming_OnQRCodeRead;
            }
        }

        private void _webcamStreaming_OnQRCodeRead(object sender, EventArgs e)
        {
            txtQRCodeData.Dispatcher.Invoke(() =>
            {
                var qrCodeData = (e as QRCodeReadEventArgs).QRCodeData;
                if (!string.IsNullOrWhiteSpace(qrCodeData))
                {
                    txtQRCodeData.Document.Blocks.Clear();
                    txtQRCodeData.Document.Blocks.Add(new Paragraph(new Run(qrCodeData)));
                    txtQRCodeData.Foreground = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    txtQRCodeData.Foreground = new SolidColorBrush(Colors.Red);
                }
            });
        }

        private void chkQRCode_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_webcamStreaming != null)
            {
                _webcamStreaming.OnQRCodeRead -= _webcamStreaming_OnQRCodeRead;
            }
        }

        private void btnClearQRCodeOutput_Click(object sender, RoutedEventArgs e)
        {
            txtQRCodeData.Document.Blocks.Clear();
        }
    }
}
