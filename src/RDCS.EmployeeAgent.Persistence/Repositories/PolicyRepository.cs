using Dapper;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.SQLite;
using System.Text.Json;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public PolicyRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<TPolicy?> GetPolicyAsync<TPolicy>(string policyType, CancellationToken cancellationToken = default) where TPolicy : class
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT PolicyJson FROM Policies 
            WHERE PolicyType = @PolicyType AND IsActive = 1
            ORDER BY DownloadedAtUtc DESC
            LIMIT 1;
        ";
        
        var result = await connection.QueryFirstOrDefaultAsync<string>(sql, new { PolicyType = policyType });
        
        if (result == null)
        {
            return null;
        }
        
        return JsonSerializer.Deserialize<TPolicy>(result);
    }

    public async Task SavePolicyAsync<TPolicy>(string policyType, TPolicy policy, CancellationToken cancellationToken = default) where TPolicy : class
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            INSERT OR REPLACE INTO Policies (
                PolicyType, PolicyJson, Version, DownloadedAtUtc, AppliedAtUtc, IsActive
            ) VALUES (
                @PolicyType, @PolicyJson, @Version, @DownloadedAtUtc, @AppliedAtUtc, @IsActive
            );
        ";
        
        var parameters = new
        {
            PolicyType = policyType,
            PolicyJson = JsonSerializer.Serialize(policy),
            Version = "1.0",
            DownloadedAtUtc = DateTime.UtcNow,
            AppliedAtUtc = DateTime.UtcNow,
            IsActive = 1
        };
        
        await connection.ExecuteAsync(sql, parameters);
        _logger.LogInformation(LogCategory.Application, "Policy {PolicyType} saved", policyType);
    }

    public async Task<bool> IsPolicyActiveAsync(string policyType, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT IsActive FROM Policies 
            WHERE PolicyType = @PolicyType
            ORDER BY DownloadedAtUtc DESC
            LIMIT 1;
        ";
        
        var result = await connection.QueryFirstOrDefaultAsync<int?>(sql, new { PolicyType = policyType });
        return result == 1;
    }
}
