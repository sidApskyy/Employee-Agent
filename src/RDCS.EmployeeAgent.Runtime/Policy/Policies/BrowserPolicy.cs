namespace RDCS.EmployeeAgent.Runtime.Policy.Policies;

public class BrowserPolicy
{
    public bool Enabled { get; set; }
    public List<string> AllowedDomains { get; set; } = new();
    public List<string> BlockedDomains { get; set; } = new();
    public bool TrackIncognito { get; set; } = false;
}
