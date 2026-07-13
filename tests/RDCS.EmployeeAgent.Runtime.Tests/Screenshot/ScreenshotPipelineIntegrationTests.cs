using Moq;
using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Runtime.EventQueue;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Policy.Policies;
using RDCS.EmployeeAgent.Runtime.Screenshot.Models;
using RDCS.EmployeeAgent.Runtime.Screenshot.Services;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Screenshot.Workers;
using RDCS.EmployeeAgent.Runtime.Storage;
using Xunit;

namespace RDCS.EmployeeAgent.Runtime.Tests.Screenshot;

public class ScreenshotPipelineIntegrationTests
{
    [Fact]
    public async Task EndToEndPipeline_ShouldCompleteSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<IAgentLogger>();
        var mockScreenshotService = new Mock<IScreenshotService>();
        var mockImageProcessingService = new Mock<IImageProcessingService>();
        var mockPolicyEngine = new Mock<IPolicyEngine>();
        var mockJobQueue = new Mock<IJobQueue>();
        var mockEventBus = new Mock<IEventBus>();
        var mockScreenshotRepository = new Mock<IScreenshotRepository>();
        var mockStorageProvider = new Mock<IStorageProvider>();
        var mockStoragePathHelper = new Mock<StoragePathHelper>();

        var policy = new ScreenshotPolicy 
        { 
            Enabled = true, 
            LocalStorageEnabled = true,
            Format = "JPEG",
            Quality = 85,
            CompressionEnabled = true
        };

        mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        var captureStream = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        mockScreenshotService.Setup(x => x.CaptureFullDesktopAsync(default))
            .ReturnsAsync(captureStream);

        var processedStream = new MemoryStream(new byte[] { 0x04, 0x05, 0x06 });
        mockImageProcessingService.Setup(x => x.ProcessImagePipelineAsync(
            It.IsAny<Stream>(), "JPEG", 85, It.IsAny<int?>(), It.IsAny<int?>(), default))
            .ReturnsAsync(processedStream);

        mockStoragePathHelper.Setup(x => x.GetBasePath())
            .Returns(@"C:\Test\Screenshots");

        mockStoragePathHelper.Setup(x => x.GetStoragePath(It.IsAny<string>(), It.IsAny<DateTime>()))
            .Returns(@"C:\Test\Screenshots\2026\07\03\EMP001");

        mockStoragePathHelper.Setup(x => x.GenerateFileName(It.IsAny<DateTime>(), "JPEG"))
            .Returns("test.jpg");

        mockStorageProvider.Setup(x => x.UploadAsync(It.IsAny<StorageRequest>(), default))
            .ReturnsAsync(new StorageResponse
            {
                Success = true,
                Key = "test.jpg",
                Url = @"C:\Test\Screenshots\2026\07\03\EMP001\test.jpg",
                SizeBytes = 3
            });

        mockStorageProvider.Setup(x => x.ExistsAsync(It.IsAny<string>(), default))
            .ReturnsAsync(true);

        var worker = new ScreenshotWorker(
            mockScreenshotService.Object,
            mockImageProcessingService.Object,
            mockPolicyEngine.Object,
            mockJobQueue.Object,
            mockEventBus.Object,
            mockScreenshotRepository.Object,
            mockStorageProvider.Object,
            mockStoragePathHelper.Object,
            mockLogger.Object
        );

        // Act
        var shouldCapture = await worker.ShouldCaptureAsync();

        // Assert
        Assert.True(shouldCapture);
        mockScreenshotService.Verify(x => x.CaptureFullDesktopAsync(default), Times.Once);
        mockImageProcessingService.Verify(x => x.ProcessImagePipelineAsync(
            It.IsAny<Stream>(), "JPEG", 85, It.IsAny<int?>(), It.IsAny<int?>(), default), Times.Once);
        mockStorageProvider.Verify(x => x.UploadAsync(It.IsAny<StorageRequest>(), default), Times.Once);
        mockScreenshotRepository.Verify(x => x.SaveAsync(It.IsAny<Screenshot>(), default), Times.Once);
        mockJobQueue.Verify(x => x.EnqueueAsync(It.IsAny<IJob>(), default), Times.Once);
    }

    [Fact]
    public async Task PolicyIntegration_ShouldRespectDisabledPolicy()
    {
        // Arrange
        var mockLogger = new Mock<IAgentLogger>();
        var mockPolicyEngine = new Mock<IPolicyEngine>();

        var policy = new ScreenshotPolicy { Enabled = false };
        mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        var worker = new ScreenshotWorker(
            Mock.Of<IScreenshotService>(),
            Mock.Of<IImageProcessingService>(),
            mockPolicyEngine.Object,
            Mock.Of<IJobQueue>(),
            Mock.Of<IEventBus>(),
            Mock.Of<IScreenshotRepository>(),
            Mock.Of<IStorageProvider>(),
            Mock.Of<StoragePathHelper>(),
            mockLogger.Object
        );

        // Act
        var shouldCapture = await worker.ShouldCaptureAsync();

        // Assert
        Assert.False(shouldCapture);
    }

    [Fact]
    public async Task StorageIntegration_ShouldStoreFileCorrectly()
    {
        // Arrange
        var mockLogger = new Mock<IAgentLogger>();
        var storagePathHelper = new StoragePathHelper();
        var storageProvider = new LocalStorageProvider(mockLogger.Object, storagePathHelper);

        var content = new MemoryStream(new byte[] { 0x01, 0x02, 0x03 });
        var request = new StorageRequest
        {
            Key = "integration/test.jpg",
            Content = content
        };

        // Act
        var result = await storageProvider.UploadAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.SizeBytes);
        Assert.True(await storageProvider.ExistsAsync("integration/test.jpg"));
    }

    [Fact]
    public async Task CleanupIntegration_ShouldDeleteOldFiles()
    {
        // Arrange
        var mockLogger = new Mock<IAgentLogger>();
        var mockScreenshotRepository = new Mock<IScreenshotRepository>();
        var mockStorageProvider = new Mock<IStorageProvider>();
        var storagePathHelper = new StoragePathHelper();
        var mockPolicyEngine = new Mock<IPolicyEngine>();
        var mockEventBus = new Mock<IEventBus>();

        var policy = new ScreenshotPolicy { AutoCleanupDays = 30 };
        mockPolicyEngine.Setup(x => x.GetPolicyAsync<ScreenshotPolicy>(default))
            .ReturnsAsync(policy);

        var cleanupWorker = new AutoCleanupWorker(
            mockScreenshotRepository.Object,
            mockStorageProvider.Object,
            storagePathHelper,
            mockPolicyEngine.Object,
            mockEventBus.Object,
            mockLogger.Object
        );

        // Act
        await cleanupWorker.CleanupOldScreenshotsAsync(default);

        // Assert
        var cutoffDate = DateTime.UtcNow.AddDays(-30);
        mockScreenshotRepository.Verify(x => x.DeleteOlderThanAsync(cutoffDate, default), Times.Once);
    }
}
