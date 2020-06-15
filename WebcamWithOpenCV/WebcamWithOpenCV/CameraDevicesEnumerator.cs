using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace WebcamWithOpenCV
{
    public static class CameraDevicesEnumerator
    {
        public static List<CameraDevice> GetAllConnectedCameras()
        {
            var cameras = new List<CameraDevice>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
            {
                foreach (var device in searcher.Get())
                {
                    cameras.Add(new CameraDevice()
                    {
                        Name = device["Caption"].ToString(),
                        Status = device["Status"].ToString(),
                        DeviceId = device["DeviceId"].ToString(),
                        PNPClass = device["PNPClass"].ToString()
                    });
                }
            }

            cameras = cameras.OrderBy(c => c.DeviceId).ToList();
            for (int i = 0; i < cameras.Count; ++i)
                cameras[i].OpenCvId = i;

            return cameras;
        }
    }
}
