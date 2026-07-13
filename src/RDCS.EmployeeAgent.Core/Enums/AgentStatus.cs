namespace RDCS.EmployeeAgent.Core.Enums;

public enum AgentStatus
{
    Unknown,
    Initializing,
    Authenticating,
    RegisteringDevice,
    AuthenticatingDevice,
    DownloadingConfiguration,
    Running,
    Paused,
    Stopping,
    Stopped,
    Error
}
