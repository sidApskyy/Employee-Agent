using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.EventQueue;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Policy.Policies;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Screenshot.Workers;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Screenshot;

public class ScreenshotWorkerTests
{
    private readonly Mock<IAgentLogger> _mockLogger;
    private readonly Mock<IScreenshotService> _mockScreenshotService;
    private readonly Mock<IImageProcessingService> _mockImageProcessingService;
    private readonly Mock<IPolicyEngine> _mockPolicyEngine;
    private readonly Mock<IJobQueue> _mockJobQueue;
    private readonly Mock<IEventBus> _mockEventBus;
    private readonly Mock<IScreenshotRepository> _mockScreenshotRepository;
    private readonly Mock<IStorageProvider> _mockStorageProvider;
    private readonly Mock<StoragePathHelper> _mockStoragePathHelper;
    private readonly ScreenshotWorker _screenshotWorker;

    public ScreenshotWorkerTests()
    {
        _mockLogger = new Mock<IAgentLogger>();
        _mockScreenshotService = new Mock<IScreenshotService>();
        _mockImageProcessingService = new Mock<IImageProcessingService>();
        _mockPolicyEngine = new Mock<IPolicyEngine>();
        _mockJobQueue = new Mock<IJobQueue>();
        _mockEventBus = new Mock<IEventBus>();
        _mockScreenshotRepository = new Mock<IScreenshotRepository>();
        _mockStorageProvider = new Mock<IStorageProvider>();
        _mockStoragePathHelper = new Mock<StoragePathHelper>();

        _screenshotWorker = new ScreenshotWorker(
            _mockScreenshotService.Object,
            _mockImageProcessingService.Object,
            _mockPolicyEngine.Object,
            _mockJobQueue.Object,
            _mockEventBus.Object,
            _mockScreenshotRepository.Object,
            _mockStorageProvider.Object,
            _mockStoragePathHelper.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task ShouldCaptureAsync_WhenDisabled_ShouldReturnFalse()
    {
        // Arrange
        var policy = new ScreenshotPolicy { Enabled = false };
        _mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        // Act
        var result = await _screenshotWorker.ShouldCaptureAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldCaptureAsync_WhenEnabled_ShouldReturnTrue()
    {
        // Arrange
        var policy = new ScreenshotPolicy { Enabled = true, LocalStorageEnabled = true };
        _mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        // Act
        var result = await _screenshotWorker.ShouldCaptureAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ShouldCaptureAsync_WhenLocalStorageDisabled_ShouldReturnFalse()
    {
        // Arrange
        var policy = new ScreenshotPolicy { Enabled = true, LocalStorageEnabled = false };
        _mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        // Act
        var result = await _screenshotWorker.ShouldCaptureAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ShouldCaptureAsync_WhenOutsideOfficeHours_ShouldReturnFalse()
    {
        // Arrange
        var policy = new ScreenshotPolicy 
        { 
            Enabled = true, 
            LocalStorageEnabled = true,
            CaptureDuringOfficeHours = true,
            OfficeHoursStart = TimeSpan.FromHours(9),
            OfficeHoursEnd = TimeSpan.FromHours(17)
        };
        _mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        // Act - run at 8 AM (outside office hours)
        var result = await _screenshotWorker.ShouldCaptureAsync();

        // Assert
        Assert.False(result);
    }
}
