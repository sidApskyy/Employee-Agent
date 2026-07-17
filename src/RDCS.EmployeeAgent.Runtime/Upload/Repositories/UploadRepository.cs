using Dapper;
using RDCS.EmployeeAgent.Persistence.SQLite;
using RDCS.EmployeeAgent.Runtime.Upload.Interfaces;
using RDCS.EmployeeAgent.Runtime.Upload.Models;

namespace RDCS.EmployeeAgent.Runtime.Upload.Repositories;

public class UploadRepository : IUploadRepository
{
    private readonly SQLiteConnectionFactory _db;

    public UploadRepository(SQLiteConnectionFactory db)
    {
        _db = db;
    }

    public async Task InsertJobAsync(UploadJob job, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            INSERT INTO UploadQueue
                (JobId, CorrelationId, EmployeeId, DeviceId, LocalFilePath, S3ObjectKey,
                 Checksum, FileSize, RetryCount, MaxRetryCount, Priority, Status,
                 CreatedAtUtc, NextRetryAtUtc, UploadedAtUtc, CompletedAtUtc, ErrorMessage, UploadId)
            VALUES
                (@JobId, @CorrelationId, @EmployeeId, @DeviceId, @LocalFilePath, @S3ObjectKey,
                 @Checksum, @FileSize, @RetryCount, @MaxRetryCount, @Priority, @Status,
                 @CreatedAtUtc, @NextRetryAtUtc, @UploadedAtUtc, @CompletedAtUtc, @ErrorMessage, @UploadId)";
        await conn.ExecuteAsync(sql, new
        {
            job.JobId, job.CorrelationId, job.EmployeeId, job.DeviceId, job.LocalFilePath, job.S3ObjectKey,
            job.Checksum, job.FileSize, job.RetryCount, job.MaxRetryCount, job.Priority,
            Status = job.Status.ToString(),
            CreatedAtUtc = job.CreatedAtUtc.ToString("o"),
            NextRetryAtUtc = job.NextRetryAtUtc?.ToString("o"),
            UploadedAtUtc = job.UploadedAtUtc?.ToString("o"),
            CompletedAtUtc = job.CompletedAtUtc?.ToString("o"),
            job.ErrorMessage, job.UploadId
        });
    }

    public async Task UpdateJobAsync(UploadJob job, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        const string sql = @"
            UPDATE UploadQueue SET
                S3ObjectKey = @S3ObjectKey,
                RetryCount = @RetryCount,
                Status = @Status,
                NextRetryAtUtc = @NextRetryAtUtc,
                UploadedAtUtc = @UploadedAtUtc,
                CompletedAtUtc = @CompletedAtUtc,
                ErrorMessage = @ErrorMessage,
                UploadId = @UploadId
            WHERE JobId = @JobId";
        await conn.ExecuteAsync(sql, new
        {
            job.S3ObjectKey, job.RetryCount,
            Status = job.Status.ToString(),
            NextRetryAtUtc = job.NextRetryAtUtc?.ToString("o"),
            UploadedAtUtc = job.UploadedAtUtc?.ToString("o"),
            CompletedAtUtc = job.CompletedAtUtc?.ToString("o"),
            job.ErrorMessage, job.UploadId, job.JobId
        });
    }

    public async Task<UploadJob?> GetJobByIdAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UploadJob>(
            "SELECT * FROM UploadQueue WHERE JobId = @jobId", new { jobId });
    }

    public async Task<List<UploadJob>> GetPendingJobsAsync(int limit = 50, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<UploadJob>(
            "SELECT * FROM UploadQueue WHERE Status = 'Pending' ORDER BY Priority DESC, CreatedAtUtc ASC LIMIT @limit",
            new { limit });
        return results.ToList();
    }

    public async Task<List<UploadJob>> GetRetryReadyJobsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var now = DateTime.UtcNow.ToString("o");
        var results = await conn.QueryAsync<UploadJob>(
            "SELECT * FROM UploadQueue WHERE Status = 'Retrying' AND NextRetryAtUtc <= @now ORDER BY Priority DESC",
            new { now });
        return results.ToList();
    }

    public async Task<List<UploadJob>> GetDeadLetterJobsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<UploadJob>(
            "SELECT * FROM DeadLetterQueue ORDER BY CreatedAt DESC LIMIT 100");
        return results.ToList();
    }

    public async Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM UploadQueue WHERE Status IN ('Pending', 'Retrying')");
    }

    public async Task RecordHistoryAsync(string jobId, string status, string? message, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO UploadHistory (JobId, Status, Message, Timestamp) VALUES (@jobId, @status, @message, @ts)",
            new { jobId, status, message, ts = DateTime.UtcNow.ToString("o") });
    }

    public async Task RecordFailureAsync(string jobId, string errorMessage, string? stackTrace, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            "INSERT INTO UploadFailures (JobId, ErrorMessage, StackTrace, OccurredAt) VALUES (@jobId, @errorMessage, @stackTrace, @ts)",
            new { jobId, errorMessage, stackTrace, ts = DateTime.UtcNow.ToString("o") });
    }

    public async Task MoveToDeadLetterAsync(UploadJob job, string reason, CancellationToken cancellationToken = default)
    {
        job.Status = UploadStatus.DeadLetter;
        var original = System.Text.Json.JsonSerializer.Serialize(job);
        var ts = DateTime.UtcNow.ToString("o");

        await _db.ExecuteInTransactionAsync(async (conn, tx) =>
        {
            await conn.ExecuteAsync(
                "INSERT INTO DeadLetterQueue (JobId, Reason, CreatedAt, OriginalJob) VALUES (@JobId, @reason, @ts, @original)",
                new { job.JobId, reason, ts, original }, tx);

            await conn.ExecuteAsync(@"
                UPDATE UploadQueue SET
                    S3ObjectKey = @S3ObjectKey, RetryCount = @RetryCount, Status = @Status,
                    NextRetryAtUtc = @NextRetryAtUtc, UploadedAtUtc = @UploadedAtUtc,
                    CompletedAtUtc = @CompletedAtUtc, ErrorMessage = @ErrorMessage, UploadId = @UploadId
                WHERE JobId = @JobId", new
            {
                job.S3ObjectKey, job.RetryCount,
                Status = job.Status.ToString(),
                NextRetryAtUtc = job.NextRetryAtUtc?.ToString("o"),
                UploadedAtUtc = job.UploadedAtUtc?.ToString("o"),
                CompletedAtUtc = job.CompletedAtUtc?.ToString("o"),
                job.ErrorMessage, job.UploadId, job.JobId
            }, tx);
        }, cancellationToken);
    }

    public async Task UpdateDailyStatisticsAsync(bool success, long bytesUploaded, long elapsedMs, CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        await conn.ExecuteAsync(@"
            INSERT INTO UploadStatistics (Date, TotalJobs, CompletedJobs, FailedJobs, TotalBytesUploaded, AverageUploadMs)
            VALUES (@today, 1, @completed, @failed, @bytes, @elapsed)
            ON CONFLICT(Date) DO UPDATE SET
                TotalJobs = TotalJobs + 1,
                CompletedJobs = CompletedJobs + @completed,
                FailedJobs = FailedJobs + @failed,
                TotalBytesUploaded = TotalBytesUploaded + @bytes,
                AverageUploadMs = (AverageUploadMs + @elapsed) / 2",
            new { today, completed = success ? 1 : 0, failed = success ? 0 : 1, bytes = bytesUploaded, elapsed = elapsedMs });
    }

    public async Task<UploadStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();

        var counts = await conn.QueryAsync<(string Status, int Count)>(
            "SELECT Status, COUNT(*) AS Count FROM UploadQueue GROUP BY Status");

        var stats = new UploadStatistics();
        foreach (var (status, count) in counts)
        {
            switch (status)
            {
                case "Pending":    stats.PendingCount = count;   break;
                case "Uploading":  stats.UploadingCount = count; break;
                case "Completed":  stats.CompletedCount = count; break;
                case "Failed":     stats.FailedCount = count;    break;
                case "Retrying":   stats.RetryingCount = count;  break;
            }
        }

        stats.DeadLetterCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM DeadLetterQueue");

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var todayStats = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM UploadStatistics WHERE Date = @today", new { today });

        if (todayStats != null)
        {
            stats.TotalBytesUploaded = todayStats.TotalBytesUploaded ?? 0;
            stats.AverageUploadMs = todayStats.AverageUploadMs ?? 0;
            int total = todayStats.TotalJobs ?? 0;
            int completed = todayStats.CompletedJobs ?? 0;
            stats.SuccessRate = total > 0 ? (double)completed / total * 100 : 0;
            stats.FailureRate = 100 - stats.SuccessRate;
        }

        return stats;
    }

    public async Task ResetStuckUploadingJobsAsync(CancellationToken cancellationToken = default)
    {
        using var conn = _db.CreateConnection();
        // Reset text-based statuses from current code
        await conn.ExecuteAsync(
            "UPDATE UploadQueue SET Status = 'Pending' WHERE Status = 'Uploading' OR Status = 'Preparing'");
        // Also fix any rows that were stored with integer enum values by prior versions
        // UploadStatus enum: 0=Pending, 1=Preparing, 2=Uploading, 3=Uploaded, 4=Completed, 5=Failed, 6=Retrying
        await conn.ExecuteAsync(
            "UPDATE UploadQueue SET Status = 'Pending' WHERE TYPEOF(Status) = 'integer' OR Status IN ('0','1','2','3','5','6')");
    }
}
