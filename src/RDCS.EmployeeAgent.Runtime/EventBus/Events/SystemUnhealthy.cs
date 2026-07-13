namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record SystemUnhealthy(
    string Message,
    DateTime Timestamp
);
