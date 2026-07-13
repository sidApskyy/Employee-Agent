using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Persistence.SQLite;
using Moq;
using Xunit;

namespace RDCS.EmployeeAgent.Tests.Repositories;

public class JobQueueRepositoryTests
{
    private readonly string _testDatabasePath;
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly JobQueueRepository _repository;

    public JobQueueRepositoryTests()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"RDCS_Test_{Guid.NewGuid()}.db");
        _connectionFactory = new SQLiteConnectionFactory(_testDatabasePath);
        _mockLogger = new Mock<IAgentLogger>();
        _repository = new JobQueueRepository(_connectionFactory, _mockLogger.Object);

        // Initialize database
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
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
        ";
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task EnqueueAsync_InsertsJob_WhenCalled()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };

        // Act
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Assert
        Assert.NotNull(jobId);
        Assert.True(int.TryParse(jobId, out _));

        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM JobQueue WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", int.Parse(jobId));
        var count = Convert.ToInt32(command.ExecuteScalar());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DequeueAsync_ReturnsJob_WhenPendingJobExists()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Act
        var job = await _repository.DequeueAsync();

        // Assert
        Assert.NotNull(job);
        Assert.Equal(int.Parse(jobId), job.Id);
        Assert.Equal("TestJob", job.JobType);
    }

    [Fact]
    public async Task DequeueAsync_ReturnsNull_WhenNoPendingJobs()
    {
        // Act
        var job = await _repository.DequeueAsync();

        // Assert
        Assert.Null(job);
    }

    [Fact]
    public async Task DequeueAsync_UpdatesJobStateToRunning_WhenJobDequeued()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Act
        await _repository.DequeueAsync();

        // Assert
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JobState FROM JobQueue WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", int.Parse(jobId));
        var state = command.ExecuteScalar()?.ToString();
        Assert.Equal("Running", state);
    }

    [Fact]
    public async Task UpdateJobStateAsync_UpdatesState_WhenCalled()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Act
        await _repository.UpdateJobStateAsync(jobId, "Completed", null);

        // Assert
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT JobState FROM JobQueue WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", int.Parse(jobId));
        var state = command.ExecuteScalar()?.ToString();
        Assert.Equal("Completed", state);
    }

    [Fact]
    public async Task UpdateJobStateAsync_IncrementsRetryCount_WhenStateIsFailed()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Act
        await _repository.UpdateJobStateAsync(jobId, "Failed", "Test error");

        // Assert
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RetryCount FROM JobQueue WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", int.Parse(jobId));
        var retryCount = Convert.ToInt32(command.ExecuteScalar());
        Assert.Equal(1, retryCount);
    }

    [Fact]
    public async Task GetJobAsync_ReturnsJob_WhenExists()
    {
        // Arrange
        var testJob = new TestJob { Name = "TestJob" };
        var jobId = await _repository.EnqueueAsync(testJob, 1);

        // Act
        var job = await _repository.GetJobAsync(jobId);

        // Assert
        Assert.NotNull(job);
        Assert.Equal(jobId, job.Id.ToString());
    }

    [Fact]
    public async Task GetJobAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var job = await _repository.GetJobAsync("99999");

        // Assert
        Assert.Null(job);
    }

    [Fact]
    public async Task GetPendingJobsAsync_ReturnsPendingJobs_WhenExist()
    {
        // Arrange
        await _repository.EnqueueAsync(new TestJob { Name = "Job1" }, 1);
        await _repository.EnqueueAsync(new TestJob { Name = "Job2" }, 1);
        await _repository.EnqueueAsync(new TestJob { Name = "Job3" }, 1);

        // Act
        var jobs = await _repository.GetPendingJobsAsync(10);

        // Assert
        Assert.Equal(3, jobs.Count);
        Assert.All(jobs, j => Assert.Equal("Pending", j.JobState));
    }

    [Fact]
    public async Task GetFailedJobsAsync_ReturnsFailedJobs_WhenExist()
    {
        // Arrange
        var jobId1 = await _repository.EnqueueAsync(new TestJob { Name = "Job1" }, 1);
        var jobId2 = await _repository.EnqueueAsync(new TestJob { Name = "Job2" }, 1);
        await _repository.UpdateJobStateAsync(jobId1, "Failed", "Error1");
        await _repository.UpdateJobStateAsync(jobId2, "Failed", "Error2");

        // Act
        var jobs = await _repository.GetFailedJobsAsync(10);

        // Assert
        Assert.Equal(2, jobs.Count);
        Assert.All(jobs, j => Assert.Equal("Failed", j.JobState));
    }

    private class TestJob
    {
        public string Name { get; set; } = string.Empty;
    }

    private void Dispose()
    {
        // Cleanup test database
        if (File.Exists(_testDatabasePath))
        {
            try
            {
                File.Delete(_testDatabasePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
