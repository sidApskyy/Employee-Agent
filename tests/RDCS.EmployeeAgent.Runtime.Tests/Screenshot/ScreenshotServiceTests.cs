using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Screenshot;

public class ScreenshotServiceTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly ScreenshotService _screenshotService;

    public ScreenshotServiceTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _screenshotService = new ScreenshotService(_mockLogger.Object);
    }

    [Fact]
    public async Task CaptureFullDesktopAsync_ShouldReturnStream()
    {
        // Act
        var result = await _screenshotService.CaptureFullDesktopAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.CanRead);
    }

    [Fact]
    public async Task GetMonitorInfoAsync_ShouldReturnMonitorList()
    {
        // Act
        var result = await _screenshotService.GetMonitorInfoAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task GetDesktopBoundsAsync_ShouldReturnBounds()
    {
        // Act
        var result = await _screenshotService.GetDesktopBoundsAsync();

        // Assert
        Assert.True(result.Width > 0);
        Assert.True(result.Height > 0);
    }
}
