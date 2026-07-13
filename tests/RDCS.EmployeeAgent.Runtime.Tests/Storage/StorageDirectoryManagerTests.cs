using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Storage;

public class StorageDirectoryManagerTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;

    public StorageDirectoryManagerTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _settings = new StorageSettings { RootPath = @"C:\Test\RDCS Agent" };
        _directoryManager = new StorageDirectoryManager(
            Microsoft.Extensions.Options.Options.Create(_settings),
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task EnsureDirectoryExistsAsync_ShouldCreateMissingFolder()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        await _directoryManager.EnsureDirectoryExistsAsync(testPath);

        // Assert
        Assert.True(Directory.Exists(testPath));

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task ValidatePathAsync_ShouldRejectTraversalAttacks()
    {
        // Act
        var result1 = await _directoryManager.ValidatePathAsync(@"C:\Test\..\Windows");
        var result2 = await _directoryManager.ValidatePathAsync(@"C:\Test\~");

        // Assert
        Assert.False(result1);
        Assert.False(result2);
    }

    [Fact]
    public async Task ValidatePathAsync_ShouldAcceptValidPath()
    {
        // Act
        var result = await _directoryManager.ValidatePathAsync(@"C:\Test\Valid\Path");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetDirectorySizeAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        var testFile = Path.Combine(testPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var size = await _directoryManager.GetDirectorySizeAsync(testPath);

        // Assert
        Assert.True(size > 0);

        // Cleanup
        Directory.Delete(testPath, true);
    }

    [Fact]
    public async Task GetFileCountAsync_ShouldCountCorrectly()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testPath);
        await File.WriteAllTextAsync(Path.Combine(testPath, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(testPath, "file2.txt"), "content2");

        // Act
        var count = await _directoryManager.GetFileCountAsync(testPath);

        // Assert
        Assert.Equal(2, count);

        // Cleanup
        Directory.Delete(testPath, true);
    }
}
