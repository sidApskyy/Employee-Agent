namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record HeartbeatCompleted(
    string DeviceId,
    DateTime Timestamp,
    TimeSpan Duration
);
