using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Policy;
using RDCS.EmployeeAgent.Runtime.Policy.Policies;
using RDCS.EmployeeAgent.Runtime.Screenshot.Events;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Storage;
using RDCS.EmployeeAgent.Runtime.Workers;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Workers;

public class AutoCleanupWorker : BackgroundWorkerBase
{
    private readonly IScreenshotRepository _screenshotRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly StoragePathHelper _storagePathHelper;
    private readonly IPolicyEngine _policyEngine;
    private readonly IEventBus _eventBus;
    private readonly IAgentLogger _logger;

    public override string Name => "AutoCleanupWorker";

    public AutoCleanupWorker(
        IScreenshotRepository screenshotRepository,
        IStorageProvider storageProvider,
        StoragePathHelper storagePathHelper,
        IPolicyEngine policyEngine,
        IEventBus eventBus,
        IAgentLogger logger) : base(logger, eventBus)
    {
        _screenshotRepository = screenshotRepository;
        _storageProvider = storageProvider;
        _storagePathHelper = storagePathHelper;
        _policyEngine = policyEngine;
        _eventBus = eventBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation(LogCategory.Application, "Auto Cleanup Worker started");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldScreenshotsAsync(cancellationToken);
                await UpdateStorageStatisticsAsync(cancellationToken);

                // Run daily
                await Task.Delay(TimeSpan.FromHours(24), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(LogCategory.Exception, "Auto Cleanup Worker error", ex);
                await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
            }
        }

        Logger.LogInformation(LogCategory.Application, "Auto Cleanup Worker stopped");
    }

    private async Task CleanupOldScreenshotsAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var policy = await _policyEngine.GetPolicyAsync<ScreenshotPolicy>(cancellationToken);
            var cutoffDate = DateTime.UtcNow.AddDays(-policy.AutoCleanupDays);

            Logger.LogInformation(LogCategory.Application, $"Starting cleanup of screenshots older than {cutoffDate}");

            // Delete from database
            await _screenshotRepository.DeleteOlderThanAsync(cutoffDate, cancellationToken);

            // Delete from storage
            var basePath = _storagePathHelper.GetBasePath();
            var deletedCount = 0;
            var freedSpaceBytes = 0L;

            if (Directory.Exists(basePath))
            {
                var directories = Directory.GetDirectories(basePath, "*", SearchOption.AllDirectories);

                foreach (var directory in directories)
                {
                    try
                    {
                        var files = Directory.GetFiles(directory, "*.*", SearchOption.TopDirectoryOnly);
                        
                        foreach (var file in files)
                        {
                            var fileInfo = new FileInfo(file);
                            if (fileInfo.CreationTimeUtc < cutoffDate)
                            {
                                var fileSize = fileInfo.Length;
                                File.Delete(file);
                                deletedCount++;
                                freedSpaceBytes += fileSize;
                            }
                        }

                        // Remove empty directories
                        if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                        {
                            Directory.Delete(directory, recursive: true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(LogCategory.Application, $"Failed to cleanup directory {directory}: {ex.Message}");
                    }
                }
            }

            stopwatch.Stop();
            Logger.LogInformation(LogCategory.Application, $"Cleanup completed: {deletedCount} files deleted, {freedSpaceBytes / (1024 * 1024)}MB freed in {stopwatch.ElapsedMilliseconds}ms");

            // Publish event
            await _eventBus.PublishAsync(new CleanupCompleted(
                deletedCount,
                freedSpaceBytes,
                DateTime.UtcNow
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Screenshot cleanup failed", ex);
        }
    }

    private async Task UpdateStorageStatisticsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var basePath = _storagePathHelper.GetBasePath();
            var totalScreenshots = 0;
            var totalSizeBytes = 0L;
            DateTime? oldestDate = null;
            DateTime? newestDate = null;

            if (Directory.Exists(basePath))
            {
                var files = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    totalScreenshots++;
                    totalSizeBytes += fileInfo.Length;

                    if (oldestDate == null || fileInfo.CreationTimeUtc < oldestDate)
                    {
                        oldestDate = fileInfo.CreationTimeUtc;
                    }

                    if (newestDate == null || fileInfo.CreationTimeUtc > newestDate)
                    {
                        newestDate = fileInfo.CreationTimeUtc;
                    }
                }
            }

            var averageSizeBytes = totalScreenshots > 0 ? (double)totalSizeBytes / totalScreenshots : 0;

            Logger.LogInformation(LogCategory.Application, $"Storage Statistics: {totalScreenshots} screenshots, {totalSizeBytes / (1024 * 1024)}MB total, avg {averageSizeBytes / 1024}KB per screenshot");
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to update storage statistics", ex);
        }
    }

    protected override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(LogCategory.Exception, "Auto Cleanup Worker error", exception);
        return Task.CompletedTask;
    }
}
