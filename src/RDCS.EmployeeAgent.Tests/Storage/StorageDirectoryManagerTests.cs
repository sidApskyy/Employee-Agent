using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Storage;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace RDCS.EmployeeAgent.Tests.Storage;

public class StorageDirectoryManagerTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _manager;

    public StorageDirectoryManagerTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _settings = new StorageSettings
        {
            RootPath = Path.Combine(Path.GetTempPath(), $"RDCS_Test_{Guid.NewGuid()}")
        };
        _manager = new StorageDirectoryManager(
            Options.Create(_settings),
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task EnsureDirectoryExistsAsync_CreatesDirectory_WhenNotExists()
    {
        // Arrange
        var testPath = Path.Combine(_settings.RootPath, "TestDir");
        
        // Act
        await _manager.EnsureDirectoryExistsAsync(testPath);
        
        // Assert
        Assert.True(Directory.Exists(testPath));
    }

    [Fact]
    public async Task EnsureDirectoryExistsAsync_DoesNotThrow_WhenDirectoryExists()
    {
        // Arrange
        var testPath = Path.Combine(_settings.RootPath, "TestDir");
        Directory.CreateDirectory(testPath);
        
        // Act & Assert
        await _manager.EnsureDirectoryExistsAsync(testPath);
        Assert.True(Directory.Exists(testPath));
    }

    [Fact]
    public async Task ValidatePathAsync_ReturnsTrue_ForValidPath()
    {
        // Arrange
        var validPath = Path.Combine(_settings.RootPath, "ValidPath");
        
        // Act
        var result = await _manager.ValidatePathAsync(validPath);
        
        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidatePathAsync_ReturnsFalse_ForPathTraversal()
    {
        // Arrange
        var invalidPath = Path.Combine(_settings.RootPath, "..", "Windows");
        
        // Act
        var result = await _manager.ValidatePathAsync(invalidPath);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidatePathAsync_ReturnsFalse_ForInvalidCharacters()
    {
        // Arrange
        var invalidPath = Path.Combine(_settings.RootPath, "Invalid\0Path");
        
        // Act
        var result = await _manager.ValidatePathAsync(invalidPath);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetDirectorySizeAsync_ReturnsZero_WhenDirectoryNotExists()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_settings.RootPath, "NonExistent");
        
        // Act
        var size = await _manager.GetDirectorySizeAsync(nonExistentPath);
        
        // Assert
        Assert.Equal(0, size);
    }

    [Fact]
    public async Task GetDirectorySizeAsync_ReturnsCorrectSize_WhenDirectoryExists()
    {
        // Arrange
        var testPath = Path.Combine(_settings.RootPath, "SizeTest");
        Directory.CreateDirectory(testPath);
        var testFile = Path.Combine(testPath, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");
        
        // Act
        var size = await _manager.GetDirectorySizeAsync(testPath);
        
        // Assert
        Assert.True(size > 0);
    }

    [Fact]
    public async Task GetFileCountAsync_ReturnsZero_WhenDirectoryNotExists()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_settings.RootPath, "NonExistent");
        
        // Act
        var count = await _manager.GetFileCountAsync(nonExistentPath);
        
        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetFileCountAsync_ReturnsCorrectCount_WhenDirectoryExists()
    {
        // Arrange
        var testPath = Path.Combine(_settings.RootPath, "CountTest");
        Directory.CreateDirectory(testPath);
        await File.WriteAllTextAsync(Path.Combine(testPath, "file1.txt"), "content1");
        await File.WriteAllTextAsync(Path.Combine(testPath, "file2.txt"), "content2");
        
        // Act
        var count = await _manager.GetFileCountAsync(testPath);
        
        // Assert
        Assert.Equal(2, count);
    }

    private void Dispose()
    {
        // Cleanup test directory
        if (Directory.Exists(_settings.RootPath))
        {
            try
            {
                Directory.Delete(_settings.RootPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
