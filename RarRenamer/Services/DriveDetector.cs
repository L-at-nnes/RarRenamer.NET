using System.IO;
using System.Management;

namespace RarRenamer.Services
{
    public static class DriveDetector
    {
        public static int GetOptimalParallelism(string folderPath, int? userOverride = null)
        {
            if (userOverride.HasValue && userOverride.Value > 0)
            {
                return userOverride.Value;
            }

            try
            {
                var driveInfo = new DriveInfo(Path.GetPathRoot(folderPath) ?? "C:\\");
                bool isSSD = IsSSD(driveInfo);
                
                int cpuCores = Environment.ProcessorCount;
                
                if (isSSD)
                {
                    return cpuCores * 4;
                }
                else
                {
                    return Math.Max(16, cpuCores);
                }
            }
            catch
            {
                return Environment.ProcessorCount * 2;
            }
        }

        private static bool IsSSD(DriveInfo drive)
        {
            try
            {
                if (!OperatingSystem.IsWindows())
                    return false;

                var driveLetter = drive.Name.TrimEnd('\\');
                
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_DiskDrive");
                
                foreach (ManagementObject wmiDrive in searcher.Get())
                {
                    using var partitionSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{wmiDrive["DeviceID"]}'}} " +
                        "WHERE AssocClass=Win32_DiskDriveToDiskPartition");
                    
                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        using var logicalSearcher = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} " +
                            "WHERE AssocClass=Win32_LogicalDiskToPartition");
                        
                        foreach (ManagementObject logical in logicalSearcher.Get())
                        {
                            if (logical["Name"]?.ToString()?.TrimEnd('\\') == driveLetter)
                            {
                                var mediaType = wmiDrive["MediaType"]?.ToString() ?? "";
                                
                                if (mediaType.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                                    mediaType.Contains("Solid State", StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                                
                                var model = wmiDrive["Model"]?.ToString() ?? "";
                                if (model.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                                    model.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                                {
                                    return true;
                                }
                                
                                return false;
                            }
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
