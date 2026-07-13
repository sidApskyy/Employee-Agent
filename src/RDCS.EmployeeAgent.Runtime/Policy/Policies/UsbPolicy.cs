namespace RDCS.EmployeeAgent.Runtime.Policy.Policies;

public class UsbPolicy
{
    public bool Enabled { get; set; }
    public bool BlockUsbStorage { get; set; } = false;
    public bool LogUsbActivity { get; set; } = true;
    public List<string> AllowedUsbDevices { get; set; } = new();
}
