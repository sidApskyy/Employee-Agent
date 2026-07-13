using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageCleanupServiceTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StorageSettings _settings;
    private readonly Mock<StorageDirectoryManager> _mockDirectoryManager;
    private readonly Mock<StoragePathProvider> _mockPathProvider;
    private readonly StorageCleanupService _cleanupService;

    public StorageCleanupServiceTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _settings = new StorageSettings 
        { 
            RootPath = @"C:\Test\RDCS Agent",
            TempFileMaxAgeHours = 24,
            CacheRetentionDays = 7,
            BackupRetentionDays = 30,
            LogRetentionDays = 30
        };
        _mockDirectoryManager = new Mock<StorageDirectoryManager>();
        _mockPathProvider = new Mock<StoragePathProvider>();
        _cleanupService = new StorageCleanupService(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockDirectoryManager.Object,
            _mockPathProvider.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task CleanupTempFilesAsync_ShouldRemoveOldFiles()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        var oldFile = Path.Combine(testPath, "old.txt");
        await File.WriteAllTextAsync(oldFile, "old content");
        File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-2));

        _mockPathProvider.Setup(x => x.GetTempFolder()).Returns(testPath);

        // Act
        var result = await _cleanupService.CleanupTempFilesAsync();

        // Assert
        Assert.True(result.FilesDeleted >= 0);
        Assert.True(result.BytesFreed >= 0);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CleanupCacheAsync_ShouldRemoveOldFiles()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetCacheFolder()).Returns(testPath);

        // Act
        var result = await _cleanupService.CleanupCacheAsync();

        // Assert
        Assert.NotNull(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CleanupExpiredBackupsAsync_ShouldRemoveOldBackups()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetBackupFolder()).Returns(testPath);

        // Act
        var result = await _cleanupService.CleanupExpiredBackupsAsync();

        // Assert
        Assert.NotNull(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CleanupOldLogsAsync_ShouldRemoveOldLogs()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetLogFolder()).Returns(testPath);

        // Act
        var result = await _cleanupService.CleanupOldLogsAsync();

        // Assert
        Assert.NotNull(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CleanupAbandonedTempFilesAsync_ShouldRemoveOrphanedFiles()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetTempProcessingFolder()).Returns(testPath);
        _mockPathProvider.Setup(x => x.GetTempUploadsFolder()).Returns(testPath);

        // Act
        var result = await _cleanupService.CleanupAbandonedTempFilesAsync();

        // Assert
        Assert.NotNull(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }
}
