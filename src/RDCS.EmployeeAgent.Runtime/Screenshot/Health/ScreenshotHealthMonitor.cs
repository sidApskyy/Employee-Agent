using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.Screenshot.Models;
using RDCS.EmployeeAgent.Runtime.Screenshot.Storage;
using RDCS.EmployeeAgent.Runtime.Storage;

namespace RDCS.EmployeeAgent.Runtime.Screenshot.Health;

public class ScreenshotHealthMonitor : IScreenshotHealthMonitor
{
    private readonly IScreenshotRepository _screenshotRepository;
    private readonly IScreenshotJobRepository _screenshotJobRepository;
    private readonly IStorageProvider _storageProvider;
    private readonly StoragePathHelper _storagePathHelper;
    private readonly IAgentLogger Logger;

    public ScreenshotHealthMonitor(
        IScreenshotRepository screenshotRepository,
        IScreenshotJobRepository screenshotJobRepository,
        IStorageProvider storageProvider,
        StoragePathHelper storagePathHelper,
        IAgentLogger logger)
    {
        _screenshotRepository = screenshotRepository;
        _screenshotJobRepository = screenshotJobRepository;
        _storageProvider = storageProvider;
        _storagePathHelper = storagePathHelper;
        Logger = logger;
    }

    public async Task<DateTime?> GetLastCaptureTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var screenshots = await _screenshotRepository.GetByEmployeeIdAsync("EMP001", cancellationToken);
            if (screenshots.Any())
            {
                return screenshots.Max(s => s.CaptureTimeUtc);
            }
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get last capture time", ex);
            return null;
        }
    }

    public async Task<double> GetAverageCaptureDurationAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This would need to be tracked separately in CaptureHistory
            // For now, return a placeholder
            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get average capture duration", ex);
            return 0;
        }
    }

    public async Task<double> GetAverageCompressionTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // This would need to be tracked separately in CompressionStats
            // For now, return a placeholder
            return 0;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get average compression time", ex);
            return 0;
        }
    }

    public async Task<int> GetQueueSizeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var pendingJobs = await _screenshotJobRepository.GetPendingJobsAsync(1000, cancellationToken);
            return pendingJobs.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get queue size", ex);
            return 0;
        }
    }

    public async Task<long> GetStorageUsageAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var basePath = _storagePathHelper.GetBasePath();
            long totalSize = 0;

            if (Directory.Exists(basePath))
            {
                var files = Directory.GetFiles(basePath, "*.*", SearchOption.AllDirectories);
                totalSize = files.Sum(f => new FileInfo(f).Length);
            }

            return totalSize;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get storage usage", ex);
            return 0;
        }
    }

    public async Task<int> GetFailureCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await _screenshotJobRepository.GetPendingJobsAsync(1000, cancellationToken);
            return jobs.Count(j => j.Status == "Failed");
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get failure count", ex);
            return 0;
        }
    }

    public async Task<double> GetSuccessRateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await _screenshotJobRepository.GetPendingJobsAsync(1000, cancellationToken);
            if (!jobs.Any())
            {
                return 100.0;
            }

            var completedJobs = jobs.Where(j => j.Status == "Completed").Count();
            var totalJobs = jobs.Count;

            return (double)completedJobs / totalJobs * 100;
        }
        catch (Exception ex)
        {
            Logger.LogError(LogCategory.Exception, "Failed to get success rate", ex);
            return 0;
        }
    }
}
