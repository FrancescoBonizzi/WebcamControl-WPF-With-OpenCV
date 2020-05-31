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
                    //Console.WriteLine($"{device["PNPClass"]} / {device["Caption"]}");
                    cameraNames.Add(device["Caption"].ToString());
                }
            }

            return cameraNames;
        }
    }
}
