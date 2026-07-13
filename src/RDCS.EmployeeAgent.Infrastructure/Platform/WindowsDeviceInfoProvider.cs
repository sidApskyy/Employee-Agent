using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace RDCS.EmployeeAgent.Infrastructure.Platform;

public class WindowsDeviceInfoProvider : IDeviceInfoProvider
{
    public async Task<DeviceInfo> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var deviceInfo = new DeviceInfo
            {
                ComputerName = Environment.MachineName,
                WindowsUsername = Environment.UserName,
                OsVersion = GetOsVersion(),
                Processor = GetProcessor(),
                RamGb = GetRamInGb(),
                DiskSizeGb = GetDiskSizeInGb(),
                MacAddress = GetMacAddress(),
                MachineGuid = GetMachineGuid(),
                AgentVersion = "1.0.0"
            };

            deviceInfo.Fingerprint = GenerateFingerprint(deviceInfo);
            deviceInfo.DeviceName = $"{deviceInfo.ComputerName}-Agent";

            return deviceInfo;
        }, cancellationToken);
    }

    private static string GetOsVersion()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
            foreach (ManagementObject os in searcher.Get())
            {
                return os["Caption"]?.ToString() ?? "Unknown";
            }
        }
        catch
        {
            return Environment.OSVersion.ToString();
        }
        return "Unknown";
    }

    private static string GetProcessor()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
            foreach (ManagementObject processor in searcher.Get())
            {
                return processor["Name"]?.ToString() ?? "Unknown";
            }
        }
        catch
        {
            return "Unknown";
        }
        return "Unknown";
    }

    private static int GetRamInGb()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
            foreach (ManagementObject sys in searcher.Get())
            {
                var bytes = Convert.ToUInt64(sys["TotalPhysicalMemory"]);
                return (int)(bytes / (1024 * 1024 * 1024));
            }
        }
        catch
        {
            return 0;
        }
        return 0;
    }

    private static int GetDiskSizeInGb()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("SELECT Size FROM Win32_LogicalDisk WHERE DeviceID='C:'");
            foreach (ManagementObject disk in searcher.Get())
            {
                var bytes = Convert.ToUInt64(disk["Size"]);
                return (int)(bytes / (1024 * 1024 * 1024));
            }
        }
        catch
        {
            return 0;
        }
        return 0;
    }

    private static string GetMacAddress()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up &&
                                   n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                   !n.Description.Contains("Virtual", StringComparison.OrdinalIgnoreCase));

            return nic?.GetPhysicalAddress().ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private static string GetMachineGuid()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography", false);
            var guid = key?.GetValue("MachineGuid")?.ToString();
            return guid ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string GenerateFingerprint(DeviceInfo deviceInfo)
    {
        var data = $"{deviceInfo.MachineGuid}|{deviceInfo.MacAddress}|{deviceInfo.Processor}|{deviceInfo.DiskSizeGb}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
