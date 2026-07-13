using Dapper;
using Microsoft.Data.Sqlite;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Persistence.SQLite;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class ScreenshotRepository : IScreenshotRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public ScreenshotRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task SaveAsync(Screenshot screenshot, CancellationToken cancellationToken = default)
    {
        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var sql = @"
                INSERT INTO Screenshots 
                (Id, EmployeeId, DeviceId, MonitorId, CaptureTimeUtc, FilePath, StoragePath, Width, Height, Format, Quality, Compressed, FileSizeBytes, CorrelationId, UploadStatus, UploadedAtUtc, CreatedAtUtc, UpdatedAtUtc)
                VALUES 
                (@Id, @EmployeeId, @DeviceId, @MonitorId, @CaptureTimeUtc, @FilePath, @StoragePath, @Width, @Height, @Format, @Quality, @Compressed, @FileSizeBytes, @CorrelationId, @UploadStatus, @UploadedAtUtc, @CreatedAtUtc, @UpdatedAtUtc)";

            await connection.ExecuteAsync(sql, screenshot, transaction);
            _logger.LogInformation(LogCategory.Database, $"Screenshot saved: {screenshot.Id}");
        }, cancellationToken);
    }

    public async Task<Screenshot?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Screenshots WHERE Id = @Id";
        return await connection.QueryFirstOrDefaultAsync<Screenshot>(sql, new { Id = id });
    }

    public async Task<List<Screenshot>> GetByEmployeeIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Screenshots WHERE EmployeeId = @EmployeeId ORDER BY CaptureTimeUtc DESC";
        var result = await connection.QueryAsync<Screenshot>(sql, new { EmployeeId = employeeId });
        return result.ToList();
    }

    public async Task<List<Screenshot>> GetByDeviceIdAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Screenshots WHERE DeviceId = @DeviceId ORDER BY CaptureTimeUtc DESC";
        var result = await connection.QueryAsync<Screenshot>(sql, new { DeviceId = deviceId });
        return result.ToList();
    }

    public async Task<List<Screenshot>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = "SELECT * FROM Screenshots WHERE CaptureTimeUtc >= @StartDate AND CaptureTimeUtc <= @EndDate ORDER BY CaptureTimeUtc DESC";
        var result = await connection.QueryAsync<Screenshot>(sql, new { StartDate = startDate, EndDate = endDate });
        return result.ToList();
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var sql = "DELETE FROM Screenshots WHERE Id = @Id";
            await connection.ExecuteAsync(sql, new { Id = id }, transaction);
            _logger.LogInformation(LogCategory.Database, $"Screenshot deleted: {id}");
        }, cancellationToken);
    }

    public async Task DeleteOlderThanAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var sql = "DELETE FROM Screenshots WHERE CaptureTimeUtc < @Date";
            var affectedRows = await connection.ExecuteAsync(sql, new { Date = date }, transaction);
            _logger.LogInformation(LogCategory.Database, $"Deleted {affectedRows} screenshots older than {date}");
        }, cancellationToken);
    }
}
