namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record AuthenticationSucceeded(
    string EmployeeId,
    string DeviceId,
    DateTime Timestamp
);
