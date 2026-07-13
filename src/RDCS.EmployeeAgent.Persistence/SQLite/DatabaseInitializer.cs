using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Data.Sqlite;

namespace RDCS.EmployeeAgent.Persistence.SQLite;

public class DatabaseInitializer
{
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly IAgentLogger _logger;

    public DatabaseInitializer(SQLiteConnectionFactory connectionFactory, IAgentLogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();

        _logger.LogInformation(LogCategory.Application, "Initializing SQLite database");

        // Enable foreign keys
        await EnableForeignKeysAsync(connection, cancellationToken);

        await CreateAgentStateTableAsync(connection, cancellationToken);
        await CreateJobQueueTableAsync(connection, cancellationToken);
        await CreatePoliciesTableAsync(connection, cancellationToken);
        await CreateFeatureFlagsTableAsync(connection, cancellationToken);
        await CreateLogsTableAsync(connection, cancellationToken);
        await CreateOfflineEventsTableAsync(connection, cancellationToken);
        await CreateQueueHistoryTableAsync(connection, cancellationToken);
        await CreateScreenshotsTableAsync(connection, cancellationToken);
        await CreateScreenshotJobsTableAsync(connection, cancellationToken);
        await CreateCompressionStatsTableAsync(connection, cancellationToken);
        await CreateCaptureHistoryTableAsync(connection, cancellationToken);
        await CreateStorageStatisticsTableAsync(connection, cancellationToken);
        await CreateUploadQueueTableAsync(connection, cancellationToken);
        await CreateUploadHistoryTableAsync(connection, cancellationToken);
        await CreateUploadFailuresTableAsync(connection, cancellationToken);
        await CreateUploadStatisticsTableAsync(connection, cancellationToken);
        await CreateDeadLetterQueueTableAsync(connection, cancellationToken);

        _logger.LogInformation(LogCategory.Application, "SQLite database initialized successfully");
    }

    private async Task EnableForeignKeysAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_keys = ON;";
        await command.ExecuteNonQueryAsync(cancellationToken);
        _logger.LogInformation(LogCategory.Application, "Foreign keys enabled");
    }

    private async Task CreateAgentStateTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS AgentState (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                AgentVersion TEXT NOT NULL,
                CurrentState TEXT NOT NULL,
                EmployeeId TEXT,
                DeviceId TEXT,
                LastHeartbeatUtc TEXT,
                LastConfigSyncUtc TEXT,
                IsOnline INTEGER DEFAULT 0,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_agentstate_currentstate ON AgentState(CurrentState);
            CREATE INDEX IF NOT EXISTS idx_agentstate_deviceid ON AgentState(DeviceId);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateJobQueueTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS JobQueue (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobType TEXT NOT NULL,
                JobPriority INTEGER DEFAULT 0,
                JobState TEXT NOT NULL,
                Payload TEXT NOT NULL,
                RetryCount INTEGER DEFAULT 0,
                MaxRetryCount INTEGER DEFAULT 3,
                Error TEXT,
                CreatedAtUtc TEXT NOT NULL,
                ScheduledAtUtc TEXT,
                StartedAtUtc TEXT,
                CompletedAtUtc TEXT,
                NextRetryAtUtc TEXT
            );
            
            CREATE INDEX IF NOT EXISTS idx_jobqueue_state ON JobQueue(JobState);
            CREATE INDEX IF NOT EXISTS idx_jobqueue_priority ON JobQueue(JobPriority);
            CREATE INDEX IF NOT EXISTS idx_jobqueue_scheduled ON JobQueue(ScheduledAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreatePoliciesTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Policies (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                PolicyType TEXT NOT NULL UNIQUE,
                PolicyJson TEXT NOT NULL,
                Version TEXT NOT NULL,
                DownloadedAtUtc TEXT NOT NULL,
                AppliedAtUtc TEXT,
                IsActive INTEGER DEFAULT 1
            );
            
            CREATE INDEX IF NOT EXISTS idx_policies_type ON Policies(PolicyType);
            CREATE INDEX IF NOT EXISTS idx_policies_active ON Policies(IsActive);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateFeatureFlagsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS FeatureFlags (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FlagName TEXT NOT NULL UNIQUE,
                IsEnabled INTEGER DEFAULT 0,
                Description TEXT,
                DownloadedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT
            );
            
            CREATE INDEX IF NOT EXISTS idx_featureflags_name ON FeatureFlags(FlagName);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateLogsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Logs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Category TEXT NOT NULL,
                Level TEXT NOT NULL,
                Message TEXT NOT NULL,
                Exception TEXT,
                Properties TEXT,
                LoggedAtUtc TEXT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_logs_category ON Logs(Category);
            CREATE INDEX IF NOT EXISTS idx_logs_level ON Logs(Level);
            CREATE INDEX IF NOT EXISTS idx_logs_loggedat ON Logs(LoggedAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateOfflineEventsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS OfflineEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EventType TEXT NOT NULL,
                EventData TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                SyncedAtUtc TEXT,
                SyncStatus TEXT DEFAULT 'Pending'
            );
            
            CREATE INDEX IF NOT EXISTS idx_offlineevents_status ON OfflineEvents(SyncStatus);
            CREATE INDEX IF NOT EXISTS idx_offlineevents_created ON OfflineEvents(CreatedAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateQueueHistoryTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS QueueHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobId INTEGER NOT NULL,
                JobType TEXT NOT NULL,
                OldState TEXT NOT NULL,
                NewState TEXT NOT NULL,
                ChangedAtUtc TEXT NOT NULL,
                Reason TEXT
            );
            
            CREATE INDEX IF NOT EXISTS idx_queuehistory_jobid ON QueueHistory(JobId);
            CREATE INDEX IF NOT EXISTS idx_queuehistory_changed ON QueueHistory(ChangedAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateScreenshotsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Screenshots (
                Id TEXT PRIMARY KEY,
                EmployeeId TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                MonitorId TEXT NOT NULL,
                CaptureTimeUtc TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                StoragePath TEXT NOT NULL,
                Width INTEGER NOT NULL,
                Height INTEGER NOT NULL,
                Format TEXT NOT NULL,
                Quality INTEGER NOT NULL,
                Compressed INTEGER NOT NULL,
                FileSizeBytes INTEGER NOT NULL,
                CorrelationId TEXT NOT NULL,
                UploadStatus TEXT NOT NULL DEFAULT 'Pending',
                UploadedAtUtc TEXT,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_screenshots_employee ON Screenshots(EmployeeId);
            CREATE INDEX IF NOT EXISTS idx_screenshots_device ON Screenshots(DeviceId);
            CREATE INDEX IF NOT EXISTS idx_screenshots_capture_time ON Screenshots(CaptureTimeUtc);
            CREATE INDEX IF NOT EXISTS idx_screenshots_upload_status ON Screenshots(UploadStatus);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateScreenshotJobsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ScreenshotJobs (
                Id TEXT PRIMARY KEY,
                CorrelationId TEXT NOT NULL,
                EmployeeId TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                MonitorId TEXT NOT NULL,
                CaptureTimeUtc TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                StoragePath TEXT NOT NULL,
                Compressed INTEGER NOT NULL,
                RetryCount INTEGER NOT NULL DEFAULT 0,
                MaxRetryCount INTEGER NOT NULL DEFAULT 3,
                Priority INTEGER NOT NULL DEFAULT 1,
                Status TEXT NOT NULL DEFAULT 'Pending',
                Error TEXT,
                CreatedAtUtc TEXT NOT NULL,
                StartedAtUtc TEXT,
                CompletedAtUtc TEXT,
                NextRetryAtUtc TEXT,
                FOREIGN KEY (CorrelationId) REFERENCES Screenshots(Id) ON DELETE CASCADE
            );
            
            CREATE INDEX IF NOT EXISTS idx_screenshot_jobs_employee ON ScreenshotJobs(EmployeeId);
            CREATE INDEX IF NOT EXISTS idx_screenshot_jobs_status ON ScreenshotJobs(Status);
            CREATE INDEX IF NOT EXISTS idx_screenshot_jobs_priority ON ScreenshotJobs(Priority);
            CREATE INDEX IF NOT EXISTS idx_screenshot_jobs_created_at ON ScreenshotJobs(CreatedAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateCompressionStatsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS CompressionStats (
                Id TEXT PRIMARY KEY,
                ScreenshotId TEXT NOT NULL,
                OriginalSizeBytes INTEGER NOT NULL,
                CompressedSizeBytes INTEGER NOT NULL,
                CompressionRatio REAL NOT NULL,
                CompressionDurationMs INTEGER NOT NULL,
                Algorithm TEXT NOT NULL,
                TimestampUtc TEXT NOT NULL,
                FOREIGN KEY (ScreenshotId) REFERENCES Screenshots(Id) ON DELETE CASCADE
            );
            
            CREATE INDEX IF NOT EXISTS idx_compression_stats_screenshot ON CompressionStats(ScreenshotId);
            CREATE INDEX IF NOT EXISTS idx_compression_stats_timestamp ON CompressionStats(TimestampUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateCaptureHistoryTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS CaptureHistory (
                Id TEXT PRIMARY KEY,
                EmployeeId TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                CaptureTimeUtc TEXT NOT NULL,
                Success INTEGER NOT NULL,
                FailureReason TEXT,
                DurationMs INTEGER NOT NULL,
                MonitorCount INTEGER NOT NULL,
                TotalSizeBytes INTEGER NOT NULL
            );
            
            CREATE INDEX IF NOT EXISTS idx_capture_history_employee ON CaptureHistory(EmployeeId);
            CREATE INDEX IF NOT EXISTS idx_capture_history_device ON CaptureHistory(DeviceId);
            CREATE INDEX IF NOT EXISTS idx_capture_history_capture_time ON CaptureHistory(CaptureTimeUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateStorageStatisticsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS StorageStatistics (
                Id TEXT PRIMARY KEY,
                TotalScreenshots INTEGER NOT NULL,
                TotalSizeBytes INTEGER NOT NULL,
                OldestScreenshotDate TEXT,
                NewestScreenshotDate TEXT,
                AverageSizeBytes REAL,
                LastCleanupTimeUtc TEXT,
                UpdatedAtUtc TEXT NOT NULL
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateUploadQueueTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS UploadQueue (
                JobId TEXT PRIMARY KEY,
                CorrelationId TEXT NOT NULL,
                EmployeeId TEXT NOT NULL,
                DeviceId TEXT NOT NULL,
                LocalFilePath TEXT NOT NULL,
                S3ObjectKey TEXT,
                Checksum TEXT NOT NULL,
                FileSize INTEGER NOT NULL,
                RetryCount INTEGER DEFAULT 0,
                MaxRetryCount INTEGER DEFAULT 5,
                Priority INTEGER DEFAULT 5,
                Status TEXT NOT NULL DEFAULT 'Pending',
                CreatedAtUtc TEXT NOT NULL,
                NextRetryAtUtc TEXT,
                UploadedAtUtc TEXT,
                CompletedAtUtc TEXT,
                ErrorMessage TEXT,
                UploadId TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_uploadqueue_status ON UploadQueue(Status);
            CREATE INDEX IF NOT EXISTS idx_uploadqueue_priority ON UploadQueue(Priority);
            CREATE INDEX IF NOT EXISTS idx_uploadqueue_employee ON UploadQueue(EmployeeId);
            CREATE INDEX IF NOT EXISTS idx_uploadqueue_retry ON UploadQueue(NextRetryAtUtc);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateUploadHistoryTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS UploadHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobId TEXT NOT NULL,
                Status TEXT NOT NULL,
                Message TEXT,
                Timestamp TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_uploadhistory_jobid ON UploadHistory(JobId);
            CREATE INDEX IF NOT EXISTS idx_uploadhistory_ts ON UploadHistory(Timestamp);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateUploadFailuresTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS UploadFailures (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobId TEXT NOT NULL,
                ErrorMessage TEXT NOT NULL,
                StackTrace TEXT,
                OccurredAt TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_uploadfailures_jobid ON UploadFailures(JobId);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateUploadStatisticsTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS UploadStatistics (
                Date TEXT PRIMARY KEY,
                TotalJobs INTEGER DEFAULT 0,
                CompletedJobs INTEGER DEFAULT 0,
                FailedJobs INTEGER DEFAULT 0,
                DeadLetterJobs INTEGER DEFAULT 0,
                TotalBytesUploaded INTEGER DEFAULT 0,
                AverageUploadMs INTEGER DEFAULT 0
            );
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task CreateDeadLetterQueueTableAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS DeadLetterQueue (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                JobId TEXT NOT NULL,
                Reason TEXT NOT NULL,
                CreatedAt TEXT NOT NULL,
                OriginalJob TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_deadletter_jobid ON DeadLetterQueue(JobId);
        ";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
