using System.Collections.Generic;
using System.IO;

namespace WTF
{
    public sealed class DriveService
    {
        public List<DriveItem> GetReadyDrives()
        {
            List<DriveItem> drives = new List<DriveItem>();

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (!driveInfo.IsReady)
                    continue;

                string label = string.IsNullOrWhiteSpace(driveInfo.VolumeLabel)
                    ? "Local Disk"
                    : driveInfo.VolumeLabel;

                drives.Add(new DriveItem
                {
                    RootPath = driveInfo.RootDirectory.FullName,
                    DisplayName = string.Format("{0} ({1})", label, driveInfo.RootDirectory.FullName)
                });
            }

            return drives;
        }
    }
}