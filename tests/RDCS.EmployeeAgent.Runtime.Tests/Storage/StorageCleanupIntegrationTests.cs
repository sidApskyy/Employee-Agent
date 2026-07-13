using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageCleanupIntegrationTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;
    private readonly StoragePathProvider _pathProvider;
    private readonly StorageCleanupService _cleanupService;
    private readonly IAgentLogger _logger;

    public StorageCleanupIntegrationTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), $"RDCS_Cleanup_Test_{Guid.NewGuid()}");
        _settings = new StorageSettings 
        { 
            RootPath = _testRootPath,
            TempFileMaxAgeHours = 1,
            CacheRetentionDays = 1,
            BackupRetentionDays = 1,
            LogRetentionDays = 1
        };
        _logger = new SerilogAgentLogger();
        _directoryManager = new StorageDirectoryManager(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _logger
        );
        _pathProvider = new StoragePathProvider(Microsoft.Extensions.Options.Options.Create(_settings));
        _cleanupService = new StorageCleanupService(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _directoryManager,
            _pathProvider,
            _logger
        );

        // Initialize storage
        var initializer = new StorageInitializer(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _directoryManager,
            _logger
        );
        initializer.InitializeAsync().Wait();
    }

    [Fact]
    public async Task EndToEndCleanup_ShouldRemoveOldFiles()
    {
        // Arrange
        var tempPath = _pathProvider.GetTempFolder();
        var oldFile = Path.Combine(tempPath, "old_file.txt");
        await File.WriteAllTextAsync(oldFile, "old content");
        File.SetCreationTimeUtc(oldFile, DateTime.UtcNow.AddDays(-2));

        var recentFile = Path.Combine(tempPath, "recent_file.txt");
        await File.WriteAllTextAsync(recentFile, "recent content");

        // Act
        var result = await _cleanupService.CleanupTempFilesAsync();

        // Assert
        Assert.True(result.FilesDeleted >= 1);
        Assert.False(File.Exists(oldFile));
        Assert.True(File.Exists(recentFile));
    }

    [Fact]
    public async Task Cleanup_ShouldPreserveRecentFiles()
    {
        // Arrange
        var cachePath = _pathProvider.GetCacheFolder();
        var recentFile = Path.Combine(cachePath, "recent.txt");
        await File.WriteAllTextAsync(recentFile, "recent content");

        // Act
        var result = await _cleanupService.CleanupCacheAsync();

        // Assert
        Assert.True(File.Exists(recentFile));
    }

    [Fact]
    public async Task Cleanup_ShouldHandleLockedFiles()
    {
        // Arrange
        var tempPath = _pathProvider.GetTempFolder();
        var lockedFile = Path.Combine(tempPath, "locked.txt");
        await using var stream = File.OpenWrite(lockedFile);
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("locked content"));

        // Act
        var result = await _cleanupService.CleanupTempFilesAsync();

        // Assert
        Assert.NotNull(result);
        // Locked file should not be deleted
        Assert.True(File.Exists(lockedFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRootPath))
        {
            try
            {
                Directory.Delete(_testRootPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
