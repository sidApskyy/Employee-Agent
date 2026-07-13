namespace RDCS.EmployeeAgent.Runtime.ModuleManager;

public enum ModuleState
{
    Unloaded,
    Loaded,
    Enabled,
    Disabled,
    Starting,
    Running,
    Stopping,
    Error
}
