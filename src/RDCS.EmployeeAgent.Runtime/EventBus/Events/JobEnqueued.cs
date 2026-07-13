namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record JobEnqueued(
    string JobId,
    string JobType,
    DateTime Timestamp
);
