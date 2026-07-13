namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class RegisterDeviceRequest
{
    public string EmployeeId { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string ComputerName { get; set; } = string.Empty;
    public string MachineGuid { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string WindowsUsername { get; set; } = string.Empty;
    public string Processor { get; set; } = string.Empty;
    public int RamGb { get; set; }
    public int DiskSizeGb { get; set; }
    public string MacAddress { get; set; } = string.Empty;
    public string AgentVersion { get; set; } = "1.0.0";
}
