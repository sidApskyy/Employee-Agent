using RDCS.EmployeeAgent.Runtime.StateMachine;

namespace RDCS.EmployeeAgent.Runtime.EventBus.Events;

public record AgentStateChanged(
    AgentState OldState,
    AgentState NewState,
    DateTime Timestamp,
    string? Reason
);
