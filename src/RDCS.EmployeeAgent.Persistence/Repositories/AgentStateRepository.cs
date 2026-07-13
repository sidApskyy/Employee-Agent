using Dapper;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.SQLite;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class AgentStateRepository : IAgentStateRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public AgentStateRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<AgentState?> GetAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT * FROM AgentState 
            ORDER BY Id DESC 
            LIMIT 1;
        ";
        
        var result = await connection.QueryFirstOrDefaultAsync<AgentState>(sql, cancellationToken);
        return result;
    }

    public async Task SaveAsync(AgentState state, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT INTO AgentState (
                AgentVersion, CurrentState, EmployeeId, DeviceId, 
                LastHeartbeatUtc, LastConfigSyncUtc, IsOnline, 
                CreatedAtUtc, UpdatedAtUtc
            ) VALUES (
                @AgentVersion, @CurrentState, @EmployeeId, @DeviceId,
                @LastHeartbeatUtc, @LastConfigSyncUtc, @IsOnline,
                @CreatedAtUtc, @UpdatedAtUtc
            );
        ";
        
        await connection.ExecuteAsync(sql, state);
        _logger.LogInformation(LogCategory.Application, "Agent state saved");
    }

    public async Task UpdateStateAsync(string newState, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            UPDATE AgentState 
            SET CurrentState = @CurrentState, UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = (SELECT Id FROM AgentState ORDER BY Id DESC LIMIT 1);
        ";
        
        await connection.ExecuteAsync(sql, new { CurrentState = newState, UpdatedAtUtc = DateTime.UtcNow });
        _logger.LogInformation(LogCategory.Application, "Agent state updated to {NewState}", newState);
    }
}
