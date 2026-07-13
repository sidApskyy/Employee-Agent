namespace RDCS.EmployeeAgent.Runtime.Policy.Policies;

public class IdlePolicy
{
    public bool Enabled { get; set; }
    public int IdleThresholdSeconds { get; set; } = 300;
    public bool PauseMonitoringOnIdle { get; set; } = true;
    public bool NotifyOnIdle { get; set; } = false;
}
