namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record ModuleStarted(
    string ModuleName,
    DateTime Timestamp
);
