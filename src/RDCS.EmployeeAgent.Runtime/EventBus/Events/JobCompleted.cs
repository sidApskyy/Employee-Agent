namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record JobCompleted(
    string JobId,
    string Result,
    DateTime Timestamp
);
