using System.Collections.Generic;
using System.Management;

namespace WebcamWithOpenCV
{
    public static class CameraDevicesEnumerator
    {
        public static List<string> GetAllConnectedCameras()
        {
            var cameraNames = new List<string>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
            {
                foreach (var device in searcher.Get())
                {
                    cameraNames.Add($"{device["Caption"]} (Type: {device["PNPClass"]})");
                }
            }

            return cameraNames;
        }
    }
}
