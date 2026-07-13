namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record ModuleUnloaded(
    string ModuleName,
    DateTime Timestamp
);
