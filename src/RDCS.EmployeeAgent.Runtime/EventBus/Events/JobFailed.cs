namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record JobFailed(
    string JobId,
    string ErrorMessage,
    DateTime Timestamp
);
