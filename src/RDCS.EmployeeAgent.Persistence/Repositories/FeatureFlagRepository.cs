using Dapper;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.SQLite;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class FeatureFlagRepository : IFeatureFlagRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public FeatureFlagRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IsEnabled FROM FeatureFlags 
            WHERE FlagName = @FlagName
            LIMIT 1;
        ";
        
        var result = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { FlagName = flagName });
        return result == 1;
    }

    public async Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT OR REPLACE INTO FeatureFlags (
                FlagName, IsEnabled, DownloadedAtUtc, UpdatedAtUtc
            ) VALUES (
                @FlagName, @IsEnabled, @DownloadedAtUtc, @UpdatedAtUtc
            );
        ";
        
        var parameters = new
        {
            FlagName = flagName,
            IsEnabled = isEnabled ? 1 : 0,
            DownloadedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        
        await connection.ExecuteAsync(sql, parameters);
        _logger.LogInformation(LogCategory.Application, "Feature flag {FlagName} set to {IsEnabled}", flagName, isEnabled);
    }

    public async Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = "SELECT * FROM FeatureFlags WHERE FlagName = @FlagName LIMIT 1;";
        
        return await connection.QueryFirstOrDefaultAsync<FeatureFlag>(sql, new { FlagName = flagName });
    }

    public async Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = "SELECT * FROM FeatureFlags ORDER BY FlagName;";
        
        var result = await connection.QueryAsync<FeatureFlag>(sql, cancellationToken);
        return result.ToList();
    }
}
