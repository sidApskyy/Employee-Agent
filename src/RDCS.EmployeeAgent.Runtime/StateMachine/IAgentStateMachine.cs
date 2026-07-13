namespace RDCS.EmployeeAgent.Runtime.StateMachine;

public interface IAgentStateMachine
{
    AgentState CurrentState { get; }
    Task TransitionToAsync(AgentState newState, string? reason = null, CancellationToken cancellationToken = default);
    Task<bool> CanTransitionToAsync(AgentState newState, CancellationToken cancellationToken = default);
    Task<List<AgentState>> GetValidTransitionsAsync(CancellationToken cancellationToken = default);
    event EventHandler<StateTransitionEventArgs>? StateChanged;
}

public class StateTransitionEventArgs : EventArgs
{
    public AgentState OldState { get; set; }
    public AgentState NewState { get; set; }
    public DateTime TransitionedAtUtc { get; set; }
    public string? Reason { get; set; }
}
