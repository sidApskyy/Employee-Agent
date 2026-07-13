using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageHealthServiceTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StorageSettings _settings;
    private readonly Mock<StorageDirectoryManager> _mockDirectoryManager;
    private readonly Mock<StoragePathProvider> _mockPathProvider;
    private readonly StorageHealthService _healthService;

    public StorageHealthServiceTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _settings = new StorageSettings { RootPath = @"C:\Test\RDCS Agent" };
        _mockDirectoryManager = new Mock<StorageDirectoryManager>();
        _mockPathProvider = new Mock<StoragePathProvider>();
        _healthService = new StorageHealthService(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockDirectoryManager.Object,
            _mockPathProvider.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetHealthStatusAsync_ShouldReturnCorrectStatus()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetRootPath()).Returns(testPath);
        _mockDirectoryManager.Setup(x => x.GetDirectorySizeAsync(It.IsAny<string>(), default))
            .ReturnsAsync(1000);
        _mockDirectoryManager.Setup(x => x.GetFileCountAsync(It.IsAny<string>(), default))
            .ReturnsAsync(5);

        // Act
        var status = await _healthService.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(status);
        Assert.True(status.RootExists);
        Assert.True(status.IsStorageAccessible);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CheckRootExistsAsync_ShouldReturnTrueForExistingRoot()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetRootPath()).Returns(testPath);

        // Act
        var result = await _healthService.CheckRootExistsAsync();

        // Assert
        Assert.True(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task CheckRootExistsAsync_ShouldReturnFalseForMissingRoot()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _mockPathProvider.Setup(x => x.GetRootPath()).Returns(testPath);

        // Act
        var result = await _healthService.CheckRootExistsAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFreeDiskSpaceAsync_ShouldReturnCorrectValue()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetRootPath()).Returns(testPath);

        // Act
        var result = await _healthService.GetFreeDiskSpaceAsync();

        // Assert
        Assert.True(result > 0);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task IsStorageAccessibleAsync_ShouldReturnTrueForAccessibleStorage()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        _mockPathProvider.Setup(x => x.GetRootPath()).Returns(testPath);

        // Act
        var result = await _healthService.IsStorageAccessibleAsync();

        // Assert
        Assert.True(result);

        // Cleanup
        Directory.Delete(testPath, true);
    }
}
