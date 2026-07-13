namespace RDCS.EmployeeAgent.Runtime.StateMachine;

public class StateTransition
{
    public AgentState FromState { get; set; }
    public AgentState ToState { get; set; }
    public DateTime TransitionedAtUtc { get; set; }
    public string? Reason { get; set; }
}
