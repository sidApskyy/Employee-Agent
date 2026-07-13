namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record PolicyUpdated(
    string PolicyType,
    DateTime Timestamp
);
