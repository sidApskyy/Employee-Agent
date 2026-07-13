using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Persistence.SQLite;
using Moq;
using Xunit;

namespace RDCS.EmployeeAgent.Tests.Repositories;

public class ScreenshotRepositoryTests
{
    private readonly string _testDatabasePath;
    private readonly SQLiteConnectionFactory _connectionFactory;
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly ScreenshotRepository _repository;

    public ScreenshotRepositoryTests()
    {
        _testDatabasePath = Path.Combine(Path.GetTempPath(), $"RDCS_Test_{Guid.NewGuid()}.db");
        _connectionFactory = new SQLiteConnectionFactory(_testDatabasePath);
        _mockLogger = new Mock<IAgentLogger>();
        _repository = new ScreenshotRepository(_connectionFactory, _mockLogger.Object);

        // Initialize database
        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
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
        ";
        command.ExecuteNonQuery();
    }

    [Fact]
    public async Task SaveAsync_InsertsScreenshot_WhenCalled()
    {
        // Arrange
        var screenshot = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = "EMP001",
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow,
            FilePath = "test.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        // Act
        await _repository.SaveAsync(screenshot);

        // Assert
        using var connection = _connectionFactory.CreateConnection();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Screenshots WHERE Id = @Id";
        command.Parameters.AddWithValue("@Id", screenshot.Id);
        var count = Convert.ToInt32(command.ExecuteScalar());
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsScreenshot_WhenExists()
    {
        // Arrange
        var screenshot = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = "EMP001",
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow,
            FilePath = "test.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _repository.SaveAsync(screenshot);

        // Act
        var result = await _repository.GetByIdAsync(screenshot.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(screenshot.Id, result.Id);
        Assert.Equal(screenshot.EmployeeId, result.EmployeeId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        // Act
        var result = await _repository.GetByIdAsync("nonexistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmployeeIdAsync_ReturnsScreenshots_WhenExists()
    {
        // Arrange
        var employeeId = "EMP001";
        var screenshot1 = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = employeeId,
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow,
            FilePath = "test1.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        var screenshot2 = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = employeeId,
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow.AddMinutes(1),
            FilePath = "test2.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _repository.SaveAsync(screenshot1);
        await _repository.SaveAsync(screenshot2);

        // Act
        var results = await _repository.GetByEmployeeIdAsync(employeeId);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.Equal(employeeId, s.EmployeeId));
    }

    [Fact]
    public async Task DeleteAsync_RemovesScreenshot_WhenExists()
    {
        // Arrange
        var screenshot = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = "EMP001",
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow,
            FilePath = "test.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _repository.SaveAsync(screenshot);

        // Act
        await _repository.DeleteAsync(screenshot.Id);

        // Assert
        var result = await _repository.GetByIdAsync(screenshot.Id);
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOlderThanAsync_RemovesOldScreenshots_WhenCalled()
    {
        // Arrange
        var oldScreenshot = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = "EMP001",
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow.AddDays(-10),
            FilePath = "old.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-10)
        };
        var newScreenshot = new Screenshot
        {
            Id = Guid.NewGuid().ToString(),
            EmployeeId = "EMP001",
            DeviceId = "DEV001",
            MonitorId = "0",
            CaptureTimeUtc = DateTime.UtcNow,
            FilePath = "new.jpg",
            StoragePath = @"C:\Screenshots",
            Width = 1920,
            Height = 1080,
            Format = "jpg",
            Quality = 85,
            Compressed = true,
            FileSizeBytes = 102400,
            CorrelationId = Guid.NewGuid().ToString(),
            UploadStatus = "Pending",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        await _repository.SaveAsync(oldScreenshot);
        await _repository.SaveAsync(newScreenshot);

        // Act
        await _repository.DeleteOlderThanAsync(DateTime.UtcNow.AddDays(-1));

        // Assert
        var oldResult = await _repository.GetByIdAsync(oldScreenshot.Id);
        var newResult = await _repository.GetByIdAsync(newScreenshot.Id);
        Assert.Null(oldResult);
        Assert.NotNull(newResult);
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
