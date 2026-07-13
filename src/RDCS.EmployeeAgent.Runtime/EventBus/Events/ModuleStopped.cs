namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record ModuleStopped(
    string ModuleName,
    DateTime Timestamp
);
