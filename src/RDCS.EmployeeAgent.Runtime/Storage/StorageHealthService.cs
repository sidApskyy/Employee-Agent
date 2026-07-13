using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Extensions.Options;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageHealthStatus
{
    public bool IsHealthy { get; set; }
    public bool RootExists { get; set; }
    public int FolderCount { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public long FreeDiskSpaceBytes { get; set; }
    public long AvailableSpaceBytes { get; set; }
    public bool IsStorageAccessible { get; set; }
    public DateTime? LastCleanupTime { get; set; }
    public List<string> Warnings { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class StorageHealthService
{
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;
    private readonly StoragePathProvider _pathProvider;
    private readonly IAgentLogger _logger;

    public StorageHealthService(
        IOptions<StorageSettings> settings,
        StorageDirectoryManager directoryManager,
        StoragePathProvider pathProvider,
        IAgentLogger logger)
    {
        _settings = settings.Value;
        _directoryManager = directoryManager;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<StorageHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var status = new StorageHealthStatus();

        try
        {
            status.RootExists = await CheckRootExistsAsync(cancellationToken);
            status.FolderCount = await GetFolderCountAsync(cancellationToken);
            status.TotalStorageUsedBytes = await GetTotalStorageUsedAsync(cancellationToken);
            status.FreeDiskSpaceBytes = await GetFreeDiskSpaceAsync(cancellationToken);
            status.AvailableSpaceBytes = status.FreeDiskSpaceBytes;
            status.IsStorageAccessible = await IsStorageAccessibleAsync(cancellationToken);
            status.LastCleanupTime = await GetLastCleanupTimeAsync(cancellationToken);

            // Determine overall health
            status.IsHealthy = status.RootExists && status.IsStorageAccessible;

            // Add warnings
            if (status.FreeDiskSpaceBytes < 1_000_000_000) // Less than 1GB
            {
                status.Warnings.Add("Free disk space is below 1GB");
            }

            if (status.TotalStorageUsedBytes > 10_000_000_000) // More than 10GB
            {
                status.Warnings.Add("Total storage used exceeds 10GB");
            }

            if (!status.RootExists)
            {
                status.Errors.Add("Root storage path does not exist");
            }

            if (!status.IsStorageAccessible)
            {
                status.Errors.Add("Storage is not accessible for read/write operations");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to get storage health status", ex);
            status.Errors.Add($"Health check failed: {ex.Message}");
            status.IsHealthy = false;
        }

        return status;
    }

    public async Task<bool> CheckRootExistsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return Directory.Exists(_settings.RootPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to check root path existence", ex);
            return false;
        }
    }

    public async Task<int> GetFolderCountAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!Directory.Exists(_settings.RootPath))
            {
                return 0;
            }

            return Directory.GetDirectories(_settings.RootPath, "*", SearchOption.AllDirectories).Length;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to count folders", ex);
            return 0;
        }
    }

    public async Task<long> GetTotalStorageUsedAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _directoryManager.GetDirectorySizeAsync(_settings.RootPath, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to calculate total storage used", ex);
            return 0;
        }
    }

    public async Task<long> GetFreeDiskSpaceAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var driveInfo = new DriveInfo(_settings.RootPath);
            return driveInfo.AvailableFreeSpace;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to get free disk space", ex);
            return 0;
        }
    }

    public async Task<long> GetAvailableSpaceAsync(CancellationToken cancellationToken = default)
    {
        return await GetFreeDiskSpaceAsync(cancellationToken);
    }

    public async Task<bool> IsStorageAccessibleAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testPath = Path.Combine(_settings.RootPath, "accessibility_test.tmp");
            await File.WriteAllTextAsync(testPath, "test", cancellationToken);
            File.Delete(testPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Storage accessibility check failed", ex);
            return false;
        }
    }

    public async Task<DateTime?> GetLastCleanupTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanupMarkerPath = Path.Combine(_settings.RootPath, ".last_cleanup");
            if (File.Exists(cleanupMarkerPath))
            {
                var content = await File.ReadAllTextAsync(cleanupMarkerPath, cancellationToken);
                if (DateTime.TryParse(content, out var lastCleanup))
                {
                    return lastCleanup;
                }
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to get last cleanup time", ex);
            return null;
        }
    }
}
