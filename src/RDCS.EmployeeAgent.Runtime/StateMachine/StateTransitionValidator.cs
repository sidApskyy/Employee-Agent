namespace RDCS.EmployeeAgent.Runtime.StateMachine;

public class StateTransitionValidator
{
    private static readonly Dictionary<AgentState, List<AgentState>> ValidTransitions = new()
    {
        { AgentState.Starting, new List<AgentState> { AgentState.Authenticating, AgentState.Disconnected } },
        { AgentState.Authenticating, new List<AgentState> { AgentState.Ready, AgentState.Disconnected } },
        { AgentState.Ready, new List<AgentState> { AgentState.Monitoring, AgentState.Paused, AgentState.Offline, AgentState.Updating, AgentState.Stopping } },
        { AgentState.Monitoring, new List<AgentState> { AgentState.Paused, AgentState.Offline, AgentState.Updating, AgentState.Stopping } },
        { AgentState.Paused, new List<AgentState> { AgentState.Monitoring, AgentState.Stopping } },
        { AgentState.Offline, new List<AgentState> { AgentState.Ready, AgentState.Stopping } },
        { AgentState.Updating, new List<AgentState> { AgentState.Ready, AgentState.Stopping } },
        { AgentState.Stopping, new List<AgentState> { AgentState.Stopped } },
        { AgentState.Stopped, new List<AgentState> { AgentState.Starting } },
        { AgentState.Disconnected, new List<AgentState> { AgentState.Starting } }
    };

    public static bool CanTransition(AgentState fromState, AgentState toState)
    {
        if (ValidTransitions.TryGetValue(fromState, out var validStates))
        {
            return validStates.Contains(toState);
        }

        return false;
    }

    public static List<AgentState> GetValidTransitions(AgentState fromState)
    {
        if (ValidTransitions.TryGetValue(fromState, out var validStates))
        {
            return new List<AgentState>(validStates);
        }

        return new List<AgentState>();
    }
}
