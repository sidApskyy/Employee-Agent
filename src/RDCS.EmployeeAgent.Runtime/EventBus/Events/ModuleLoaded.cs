namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record ModuleLoaded(
    string ModuleName,
    DateTime Timestamp
);
