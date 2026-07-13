namespace RDCS.EmployeeAgent.Runtime.Policy.Policies;

public class ApplicationPolicy
{
    public bool Enabled { get; set; }
    public List<string> MonitoredApplications { get; set; } = new();
    public List<string> BlockedApplications { get; set; } = new();
    public bool TrackIdleTime { get; set; } = true;
}
