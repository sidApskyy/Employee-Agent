using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Screenshot;

public class CompressionProviderTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly JpegCompressionProvider _jpegProvider;
    private readonly PngCompressionProvider _pngProvider;

    public CompressionProviderTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _jpegProvider = new JpegCompressionProvider(_mockLogger.Object);
        _pngProvider = new PngCompressionProvider(_mockLogger.Object);
    }

    [Fact]
    public void JpegProvider_ShouldReturnCorrectName()
    {
        Assert.Equal("JPEG", _jpegProvider.ProviderName);
    }

    [Fact]
    public void PngProvider_ShouldReturnCorrectName()
    {
        Assert.Equal("PNG", _pngProvider.ProviderName);
    }

    [Fact]
    public void JpegProvider_ShouldSupportJpgFormats()
    {
        Assert.Contains("jpg", _jpegProvider.SupportedFormats);
        Assert.Contains("jpeg", _jpegProvider.SupportedFormats);
    }

    [Fact]
    public void PngProvider_ShouldSupportPngFormat()
    {
        Assert.Contains("png", _pngProvider.SupportedFormats);
    }

    [Fact]
    public async Task JpegProvider_CompressAsync_ShouldReturnStream()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        // Act
        var result = await _jpegProvider.CompressAsync(content, 85);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanRead);
    }

    [Fact]
    public async Task PngProvider_CompressAsync_ShouldReturnStream()
    {
        // Arrange
        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });

        // Act
        var result = await _pngProvider.CompressAsync(content, 85);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanRead);
    }
}
