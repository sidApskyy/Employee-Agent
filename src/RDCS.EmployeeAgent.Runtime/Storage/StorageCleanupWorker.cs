using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.Workers;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageCleanupWorker : BackgroundWorkerBase
{
    private readonly StorageCleanupService _cleanupService;
    private readonly StorageHealthService _healthService;
    private readonly StorageSettings _settings;

    public override string Name => "StorageCleanupWorker";

    public StorageCleanupWorker(
        StorageCleanupService cleanupService,
        StorageHealthService healthService,
        Microsoft.Extensions.Options.IOptions<StorageSettings> settings,
        IAgentLogger logger,
        IEventBus eventBus) : base(logger, eventBus)
    {
        _cleanupService = cleanupService;
        _healthService = healthService;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation(LogCategory.Application, "Storage Cleanup Worker started");

        // Run crash recovery on startup
        await RunCrashRecoveryAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunScheduledCleanupAsync(cancellationToken);

                // Wait for next interval
                var interval = TimeSpan.FromHours(_settings.TempCleanupIntervalHours);
                await Task.Delay(interval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Logger.LogError(LogCategory.Exception, "Storage Cleanup Worker error", ex);
                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            }
        }

        Logger.LogInformation(LogCategory.Application, "Storage Cleanup Worker stopped");
    }

    private async Task RunCrashRecoveryAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation(LogCategory.Application, "Running crash recovery cleanup");
            await _cleanupService.CleanupAbandonedTempFilesAsync(cancellationToken);
            await _cleanupService.UpdateLastCleanupTimeAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Crash recovery cleanup failed", ex);
        }
    }

    private async Task RunScheduledCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            Logger.LogInformation(LogCategory.Application, "Starting scheduled storage cleanup");

            // Check health before cleanup
            var healthStatus = await _healthService.GetHealthStatusAsync(cancellationToken);
            if (!healthStatus.IsStorageAccessible)
            {
                Logger.LogWarning(LogCategory.Application, "Storage not accessible, skipping cleanup");
                return;
            }

            // Run all cleanup operations
            var tempResult = await _cleanupService.CleanupTempFilesAsync(cancellationToken);
            var cacheResult = await _cleanupService.CleanupCacheAsync(cancellationToken);
            var backupResult = await _cleanupService.CleanupExpiredBackupsAsync(cancellationToken);
            var logResult = await _cleanupService.CleanupOldLogsAsync(cancellationToken);

            // Update last cleanup time
            await _cleanupService.UpdateLastCleanupTimeAsync(cancellationToken);

            // Log summary
            var totalFilesDeleted = tempResult.FilesDeleted + cacheResult.FilesDeleted + backupResult.FilesDeleted + logResult.FilesDeleted;
            var totalBytesFreed = tempResult.BytesFreed + cacheResult.BytesFreed + backupResult.BytesFreed + logResult.BytesFreed;

            Logger.LogInformation(LogCategory.Application,
                $"Scheduled cleanup completed: {totalFilesDeleted} files deleted, {totalBytesFreed / (1024 * 1024)}MB freed");

            // Check for errors
            var allErrors = new List<string>();
            allErrors.AddRange(tempResult.Errors);
            allErrors.AddRange(cacheResult.Errors);
            allErrors.AddRange(backupResult.Errors);
            allErrors.AddRange(logResult.Errors);

            if (allErrors.Any())
            {
                Logger.LogWarning(LogCategory.Application, $"Cleanup completed with {allErrors.Count} errors");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Scheduled cleanup failed", ex);
        }
    }

    protected override Task OnErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        Logger.LogError(LogCategory.Exception, "Storage Cleanup Worker error", exception);
        return Task.CompletedTask;
    }
}
