using RDCS.EmployeeAgent.Core.Models;
using System.Security.Cryptography;
using System.Text;

namespace RDCS.EmployeeAgent.Services.DeviceRegistration;

public static class DeviceFingerprintBuilder
{
    public static string BuildFingerprint(DeviceInfo deviceInfo)
    {
        var data = $"{deviceInfo.MachineGuid}|{deviceInfo.MacAddress}|{deviceInfo.Processor}|{deviceInfo.DiskSizeGb}";
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
