using Dapper;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.SQLite;
using System.Text.Json;

namespace RDCS.EmployeeAgent.Persistence.Repositories;

public class JobQueueRepository : IJobQueueRepository
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public JobQueueRepository(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<string> EnqueueAsync<T>(T job, int priority, CancellationToken cancellationToken = default)
    {
        return await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            const string sql = @"
                INSERT INTO JobQueue (
                    JobType, JobPriority, JobState, Payload,
                    RetryCount, MaxRetryCount, CreatedAtUtc
                ) VALUES (
                    @JobType, @JobPriority, @JobState, @Payload,
                    @RetryCount, @MaxRetryCount, @CreatedAtUtc
                );

                SELECT last_insert_rowid();
            ";

            var payload = JsonSerializer.Serialize(job);
            var parameters = new
            {
                JobType = typeof(T).Name,
                JobPriority = priority,
                JobState = "Pending",
                Payload = payload,
                RetryCount = 0,
                MaxRetryCount = 3,
                CreatedAtUtc = DateTime.UtcNow
            };

            var jobId = await connection.QuerySingleAsync<int>(sql, parameters, transaction);
            _logger.LogInformation(LogCategory.Application, "Job enqueued: {JobType} with ID {JobId}", typeof(T).Name, jobId);

            return jobId.ToString();
        }, cancellationToken);
    }

    public async Task<JobQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT * FROM JobQueue 
            WHERE JobState = 'Pending' AND (ScheduledAtUtc IS NULL OR ScheduledAtUtc <= datetime('now'))
            ORDER BY JobPriority DESC, CreatedAtUtc ASC
            LIMIT 1;
        ";
        
        var job = await connection.QueryFirstOrDefaultAsync<JobQueueItem>(sql);
        
        if (job != null)
        {
            const string updateSql = @"
                UPDATE JobQueue 
                SET JobState = 'Running', StartedAtUtc = @StartedAtUtc
                WHERE Id = @Id;
            ";
            
            await connection.ExecuteAsync(updateSql, new { Id = job.Id, StartedAtUtc = DateTime.UtcNow });
        }
        
        return job;
    }

    public async Task UpdateJobStateAsync(string jobId, string newState, string? error = null, CancellationToken cancellationToken = default)
    {
        await _connectionFactory.ExecuteInTransactionAsync(async (connection, transaction) =>
        {
            var sql = newState switch
            {
                "Completed" => @"
                    UPDATE JobQueue
                    SET JobState = @JobState, CompletedAtUtc = @CompletedAtUtc
                    WHERE Id = @Id;
                ",
                "Failed" => @"
                    UPDATE JobQueue
                    SET JobState = @JobState, Error = @Error, RetryCount = RetryCount + 1
                    WHERE Id = @Id;
                ",
                "Retrying" => @"
                    UPDATE JobQueue
                    SET JobState = @JobState, NextRetryAtUtc = @NextRetryAtUtc
                    WHERE Id = @Id;
                ",
                _ => @"
                    UPDATE JobQueue
                    SET JobState = @JobState
                    WHERE Id = @Id;
                "
            };

            var parameters = new
            {
                Id = int.Parse(jobId),
                JobState = newState,
                Error = error,
                CompletedAtUtc = DateTime.UtcNow,
                NextRetryAtUtc = DateTime.UtcNow.AddSeconds(30)
            };

            await connection.ExecuteAsync(sql, parameters, transaction);
            _logger.LogInformation(LogCategory.Application, "Job {JobId} state updated to {NewState}", jobId, newState);
        }, cancellationToken);
    }

    public async Task<JobQueueItem?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = "SELECT * FROM JobQueue WHERE Id = @Id;";
        
        return await connection.QueryFirstOrDefaultAsync<JobQueueItem>(sql, new { Id = int.Parse(jobId) });
    }

    public async Task<List<JobQueueItem>> GetPendingJobsAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT * FROM JobQueue 
            WHERE JobState = 'Pending'
            ORDER BY JobPriority DESC, CreatedAtUtc ASC
            LIMIT @Limit;
        ";
        
        var result = await connection.QueryAsync<JobQueueItem>(sql, new { Limit = limit });
        return result.ToList();
    }

    public async Task<List<JobQueueItem>> GetFailedJobsAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        
        const string sql = @"
            SELECT * FROM JobQueue 
            WHERE JobState = 'Failed'
            ORDER BY CreatedAtUtc DESC
            LIMIT @Limit;
        ";
        
        var result = await connection.QueryAsync<JobQueueItem>(sql, new { Limit = limit });
        return result.ToList();
    }
}
