using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageInitializationTests : IDisposable
{
    private readonly string _testRootPath;
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;
    private readonly StoragePathProvider _pathProvider;
    private readonly IStorageInitializer _initializer;
    private readonly IAgentLogger _logger;

    public StorageInitializationTests()
    {
        _testRootPath = Path.Combine(Path.GetTempPath(), $"RDCS_Test_{Guid.NewGuid()}");
        _settings = new StorageSettings { RootPath = _testRootPath };
        _logger = new SerilogAgentLogger();
        _directoryManager = new StorageDirectoryManager(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _logger
        );
        _pathProvider = new StoragePathProvider(Microsoft.Extensions.Options.Options.Create(_settings));
        _initializer = new StorageInitializer(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _directoryManager,
            _logger
        );
    }

    [Fact]
    public async Task FullInitialization_ShouldCreateAllFolders()
    {
        // Act
        var result = await _initializer.InitializeAsync();

        // Assert
        Assert.True(result);
        Assert.True(Directory.Exists(_testRootPath));
        Assert.True(Directory.Exists(_pathProvider.GetScreenshotFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetQueueFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetQueuePendingFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetQueueProcessingFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetQueueFailedFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetQueueArchiveFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetDatabaseFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetLogFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetApplicationLogFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetErrorLogFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetPerformanceLogFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetTempFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetCacheFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetConfigFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetDiagnosticsFolder()));
        Assert.True(Directory.Exists(_pathProvider.GetBackupFolder()));
    }

    [Fact]
    public async Task Initialization_ShouldHandleExistingFolders()
    {
        // Arrange
        Directory.CreateDirectory(_testRootPath);
        Directory.CreateDirectory(_pathProvider.GetScreenshotFolder());

        // Act
        var result = await _initializer.InitializeAsync();

        // Assert
        Assert.True(result);
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
