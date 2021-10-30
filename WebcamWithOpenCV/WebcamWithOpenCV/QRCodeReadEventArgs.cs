using System;

namespace WebcamWithOpenCV
{
    public class QRCodeReadEventArgs : EventArgs
    {
        public QRCodeReadEventArgs(string qRCodeData) => QRCodeData = qRCodeData;

        public string QRCodeData { get; }
    }
}
