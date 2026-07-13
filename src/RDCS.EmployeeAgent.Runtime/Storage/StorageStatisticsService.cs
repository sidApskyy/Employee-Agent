using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class FolderStatistics
{
    public string FolderName { get; set; }
    public string Path { get; set; }
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public int FolderCount { get; set; }
    public DateTime? OldestFileDate { get; set; }
    public DateTime? NewestFileDate { get; set; }
}

public class OverallStatistics
{
    public long TotalSizeBytes { get; set; }
    public int TotalFileCount { get; set; }
    public int TotalFolderCount { get; set; }
    public Dictionary<string, FolderStatistics> FolderStatistics { get; set; } = new();
}

public class StorageStatisticsService
{
    private readonly StorageDirectoryManager _directoryManager;
    private readonly StoragePathProvider _pathProvider;
    private readonly IAgentLogger _logger;

    public StorageStatisticsService(
        StorageDirectoryManager directoryManager,
        StoragePathProvider pathProvider,
        IAgentLogger logger)
    {
        _directoryManager = directoryManager;
        _pathProvider = pathProvider;
        _logger = logger;
    }

    public async Task<FolderStatistics> GetScreenshotStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetScreenshotFolder(), "Screenshots", cancellationToken);
    }

    public async Task<FolderStatistics> GetQueueStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetQueueFolder(), "Queue", cancellationToken);
    }

    public async Task<FolderStatistics> GetDatabaseStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetDatabaseFolder(), "Database", cancellationToken);
    }

    public async Task<FolderStatistics> GetLogStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetLogFolder(), "Logs", cancellationToken);
    }

    public async Task<FolderStatistics> GetTempStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetTempFolder(), "Temp", cancellationToken);
    }

    public async Task<FolderStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await GetFolderStatisticsAsync(_pathProvider.GetCacheFolder(), "Cache", cancellationToken);
    }

    public async Task<OverallStatistics> GetOverallStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var overall = new OverallStatistics();

        var folders = new[]
        {
            ("Screenshots", _pathProvider.GetScreenshotFolder()),
            ("Queue", _pathProvider.GetQueueFolder()),
            ("Database", _pathProvider.GetDatabaseFolder()),
            ("Logs", _pathProvider.GetLogFolder()),
            ("Temp", _pathProvider.GetTempFolder()),
            ("Cache", _pathProvider.GetCacheFolder()),
            ("Config", _pathProvider.GetConfigFolder()),
            ("Diagnostics", _pathProvider.GetDiagnosticsFolder()),
            ("Backups", _pathProvider.GetBackupFolder())
        };

        foreach (var (name, path) in folders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stats = await GetFolderStatisticsAsync(path, name, cancellationToken);
            overall.FolderStatistics[name] = stats;
            overall.TotalSizeBytes += stats.SizeBytes;
            overall.TotalFileCount += stats.FileCount;
            overall.TotalFolderCount += stats.FolderCount;
        }

        return overall;
    }

    private async Task<FolderStatistics> GetFolderStatisticsAsync(string path, string name, CancellationToken cancellationToken)
    {
        var stats = new FolderStatistics
        {
            FolderName = name,
            Path = path
        };

        try
        {
            if (!Directory.Exists(path))
            {
                return stats;
            }

            stats.SizeBytes = await _directoryManager.GetDirectorySizeAsync(path, cancellationToken);
            stats.FileCount = await _directoryManager.GetFileCountAsync(path, cancellationToken);
            stats.FolderCount = Directory.GetDirectories(path, "*", SearchOption.AllDirectories).Length;

            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            if (files.Length > 0)
            {
                stats.OldestFileDate = files.Select(f => new FileInfo(f).CreationTimeUtc).Min();
                stats.NewestFileDate = files.Select(f => new FileInfo(f).CreationTimeUtc).Max();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, $"Failed to get statistics for folder: {path}", ex);
        }

        return stats;
    }
}
