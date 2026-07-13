using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Screenshot;

public class LocalStorageProviderTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly StoragePathHelper _storagePathHelper;
    private readonly LocalStorageProvider _localStorageProvider;

    public LocalStorageProviderTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _storagePathHelper = new StoragePathHelper();
        _localStorageProvider = new LocalStorageProvider(_mockLogger.Object, _storagePathHelper);
    }

    [Fact]
    public async Task UploadAsync_ShouldSaveFile()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var request = new StorageRequest
        {
            Key = "test/test.jpg",
            Content = content
        };

        // Act
        var result = await _localStorageProvider.UploadAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.SizeBytes);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrueForExistingFile()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var request = new StorageRequest
        {
            Key = "test/exists.jpg",
            Content = content
        };
        await _localStorageProvider.UploadAsync(request);

        // Act
        var result = await _localStorageProvider.ExistsAsync("test/exists.jpg");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveFile()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var request = new StorageRequest
        {
            Key = "test/delete.jpg",
            Content = content
        };
        await _localStorageProvider.UploadAsync(request);

        // Act
        await _localStorageProvider.DeleteAsync("test/delete.jpg");

        // Assert
        var exists = await _localStorageProvider.ExistsAsync("test/delete.jpg");
        Assert.False(exists);
    }
}
