using Dapper;
using Microsoft.Data.Sqlite;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Persistence.SQLite;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class ScreenshotJobRepository : IScreenshotJobRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public ScreenshotJobRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task SaveAsync(ScreenshotJob job, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            INSERT INTO ScreenshotJobs 
            (Id, CorrelationId, EmployeeId, DeviceId, MonitorId, CaptureTimeUtc, FilePath, StoragePath, Compressed, RetryCount, MaxRetryCount, Priority, Status, Error, CreatedAtUtc, StartedAtUtc, CompletedAtUtc, NextRetryAtUtc)
            VALUES 
            (@Id, @CorrelationId, @EmployeeId, @DeviceId, @MonitorId, @CaptureTimeUtc, @FilePath, @StoragePath, @Compressed, @RetryCount, @MaxRetryCount, @Priority, @Status, @Error, @CreatedAtUtc, @StartedAtUtc, @CompletedAtUtc, @NextRetryAtUtc)";

        await connection.ExecuteAsync(sql, job);
        _logger.LogInformation(LogCategory.Database, $"ScreenshotJob saved: {job.Id}");
    }

    public async Task<ScreenshotJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM ScreenshotJobs WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<ScreenshotJob>(sql, new { Id = id });
    }

    public async Task<List<ScreenshotJob>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            SELECT * FROM ScreenshotJobs 
            WHERE Status = 'Pending' 
            ORDER BY Priority DESC, CreatedAtUtc ASC 
            LIMIT @Limit";

        var result = await connection.QueryAsync<ScreenshotJob>(sql, new { Limit = limit });
        return result.ToList();
    }

    public async Task UpdateStatusAsync(string id, string status, string? error, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE ScreenshotJobs 
            SET Status = @Status, Error = @Error, UpdatedAtUtc = @UpdatedAtUtc
            WHERE Id = @Id";

        await connection.ExecuteAsync(sql, new { Id = id, Status = status, Error = error, UpdatedAtUtc = DateTime.UtcNow });
        _logger.LogInformation(LogCategory.Database, $"ScreenshotJob {id} status updated to {status}");
    }

    public async Task IncrementRetryAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = @"
            UPDATE ScreenshotJobs 
            SET RetryCount = RetryCount + 1, NextRetryAtUtc = @NextRetryAtUtc
            WHERE Id = @Id";

        var nextRetryAt = DateTime.UtcNow.AddMinutes(5);
        await connection.ExecuteAsync(sql, new { Id = id, NextRetryAtUtc = nextRetryAt });
        _logger.LogInformation(LogCategory.Database, $"ScreenshotJob {id} retry incremented");
    }
}
