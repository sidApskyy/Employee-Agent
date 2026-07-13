namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record AgentStarted(
    string AgentVersion,
    DateTime Timestamp
);
