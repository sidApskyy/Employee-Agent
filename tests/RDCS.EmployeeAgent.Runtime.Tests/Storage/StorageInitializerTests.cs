using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageInitializerTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StorageSettings _settings;
    private readonly Mock<StorageDirectoryManager> _mockDirectoryManager;

    public StorageInitializerTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _settings = new StorageSettings { RootPath = @"C:\Test\RDCS Agent" };
        _mockDirectoryManager = new Mock<StorageDirectoryManager>();
    }

    [Fact]
    public async Task InitializeAsync_ShouldCreateAllFolders()
    {
        // Arrange
        var initializer = new StorageInitializer(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockDirectoryManager.Object,
            _mockLogger.Object
        );

        // Act
        var result = await initializer.InitializeAsync();

        // Assert
        Assert.True(result);
        _mockDirectoryManager.Verify(x => x.EnsureDirectoryExistsAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ValidateStorageAsync_ShouldReturnTrueForValidStorage()
    {
        // Arrange
        _mockDirectoryManager.Setup(x => x.ValidatePathAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);
        
        var initializer = new StorageInitializer(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockDirectoryManager.Object,
            _mockLogger.Object
        );

        // Act
        var result = await initializer.ValidateStorageAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateStorageAsync_ShouldReturnFalseForInvalidStorage()
    {
        // Arrange
        _mockDirectoryManager.Setup(x => x.ValidatePathAsync(It.IsAny<string>(), default))
            .ReturnsAsync(false);
        
        var initializer = new StorageInitializer(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockDirectoryManager.Object,
            _mockLogger.Object
        );

        // Act
        var result = await initializer.ValidateStorageAsync();

        // Assert
        Assert.False(result);
    }
}
