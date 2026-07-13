namespace RDCS.EmployeeAgent.Runtime.StateMachine;

public enum AgentState
{
    Starting,
    Authenticating,
    Ready,
    Monitoring,
    Paused,
    Offline,
    Updating,
    Stopping,
    Stopped,
    Disconnected
}
