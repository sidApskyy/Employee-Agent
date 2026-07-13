namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IAgentStateRepository
{
    Task<AgentState?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AgentState state, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(string newState, CancellationToken cancellationToken = default);
}

public class AgentState
{
    public int Id { get; set; }
    public string AgentVersion { get; set; } = string.Empty;
    public string CurrentState { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? DeviceId { get; set; }
    public DateTime? LastHeartbeatUtc { get; set; }
    public DateTime? LastConfigSyncUtc { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
