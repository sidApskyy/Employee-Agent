using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;

namespace RDCS.EmployeeAgent.Runtime.StateMachine;

public class AgentStateMachine : IAgentStateMachine
{
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly object _stateLock = new();
    private AgentState _currentState = AgentState.Stopped;

    public AgentState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
    }

    public event EventHandler<StateTransitionEventArgs>? StateChanged;

    public AgentStateMachine(IAgentLogger logger, IEventBus eventBus)
    {
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task TransitionToAsync(AgentState newState, string? reason = null, CancellationToken cancellationToken = default)
    {
        AgentState oldState;
        
        lock (_stateLock)
        {
            oldState = _currentState;
            
            if (!StateTransitionValidator.CanTransition(oldState, newState))
            {
                throw new InvalidStateTransitionException(oldState, newState);
            }
            
            _currentState = newState;
        }

        var eventArgs = new StateTransitionEventArgs
        {
            OldState = oldState,
            NewState = newState,
            TransitionedAtUtc = DateTime.UtcNow,
            Reason = reason
        };

        StateChanged?.Invoke(this, eventArgs);

        _logger.LogInformation(LogCategory.Application, 
            "State transition: {OldState} -> {NewState} (Reason: {Reason})", 
            oldState, newState, reason ?? "None");

        // Publish state change event
        await _eventBus.PublishAsync(new AgentStateChanged(oldState, newState, DateTime.UtcNow, reason), cancellationToken);
    }

    public Task<bool> CanTransitionToAsync(AgentState newState, CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            return Task.FromResult(StateTransitionValidator.CanTransition(_currentState, newState));
        }
    }

    public Task<List<AgentState>> GetValidTransitionsAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            return Task.FromResult(StateTransitionValidator.GetValidTransitions(_currentState));
        }
    }
}

public class InvalidStateTransitionException : Exception
{
    public AgentState FromState { get; }
    public AgentState ToState { get; }

    public InvalidStateTransitionException(AgentState fromState, AgentState toState)
        : base($"Invalid state transition from {fromState} to {toState}")
    {
        FromState = fromState;
        ToState = toState;
    }
}
