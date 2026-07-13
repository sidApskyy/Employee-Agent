using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using Microsoft.Extensions.Options;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class CleanupResult
{
    public int FilesDeleted { get; set; }
    public long BytesFreed { get; set; }
    public List<string> Errors { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class StorageCleanupService
{
    private readonly StorageSettings _settings;
    private readonly StorageDirectoryManager _directoryManager;
    private readonly StoragePathProvider _pathProvider;
    private readonly IAgentLogger _logger;

    public StorageCleanupService(
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

    public async Task<CleanupResult> CleanupTempFilesAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-_settings.TempFileMaxAgeHours);
            _logger.LogInformation(LogCategory.Application, $"Starting temp file cleanup (older than {cutoffDate})");

            await CleanupDirectoryAsync(_pathProvider.GetTempFolder(), cutoffDate, result, cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            _logger.LogInformation(LogCategory.Application, $"Temp file cleanup completed: {result.FilesDeleted} files deleted, {result.BytesFreed / (1024 * 1024)}MB freed in {result.Duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Temp file cleanup failed", ex);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<CleanupResult> CleanupCacheAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_settings.CacheRetentionDays);
            _logger.LogInformation(LogCategory.Application, $"Starting cache cleanup (older than {cutoffDate})");

            await CleanupDirectoryAsync(_pathProvider.GetCacheFolder(), cutoffDate, result, cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            _logger.LogInformation(LogCategory.Application, $"Cache cleanup completed: {result.FilesDeleted} files deleted, {result.BytesFreed / (1024 * 1024)}MB freed in {result.Duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Cache cleanup failed", ex);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<CleanupResult> CleanupExpiredBackupsAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_settings.BackupRetentionDays);
            _logger.LogInformation(LogCategory.Application, $"Starting backup cleanup (older than {cutoffDate})");

            await CleanupDirectoryAsync(_pathProvider.GetBackupFolder(), cutoffDate, result, cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            _logger.LogInformation(LogCategory.Application, $"Backup cleanup completed: {result.FilesDeleted} files deleted, {result.BytesFreed / (1024 * 1024)}MB freed in {result.Duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Backup cleanup failed", ex);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<CleanupResult> CleanupOldLogsAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-_settings.LogRetentionDays);
            _logger.LogInformation(LogCategory.Application, $"Starting log cleanup (older than {cutoffDate})");

            await CleanupDirectoryAsync(_pathProvider.GetLogFolder(), cutoffDate, result, cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            _logger.LogInformation(LogCategory.Application, $"Log cleanup completed: {result.FilesDeleted} files deleted, {result.BytesFreed / (1024 * 1024)}MB freed in {result.Duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Log cleanup failed", ex);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    public async Task<CleanupResult> CleanupAbandonedTempFilesAsync(CancellationToken cancellationToken = default)
    {
        var result = new CleanupResult();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-_settings.TempFileMaxAgeHours);
            _logger.LogInformation(LogCategory.Application, $"Starting abandoned temp file cleanup (older than {cutoffDate})");

            // Clean up temp files that are not referenced by any active process
            await CleanupDirectoryAsync(_pathProvider.GetTempProcessingFolder(), cutoffDate, result, cancellationToken);
            await CleanupDirectoryAsync(_pathProvider.GetTempUploadsFolder(), cutoffDate, result, cancellationToken);

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            _logger.LogInformation(LogCategory.Application, $"Abandoned temp file cleanup completed: {result.FilesDeleted} files deleted, {result.BytesFreed / (1024 * 1024)}MB freed in {result.Duration.TotalMilliseconds}ms");
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Abandoned temp file cleanup failed", ex);
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task CleanupDirectoryAsync(string path, DateTime cutoffDate, CleanupResult result, CancellationToken cancellationToken)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    var fileSize = fileInfo.Length;
                    File.Delete(file);
                    result.FilesDeleted++;
                    result.BytesFreed += fileSize;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogCategory.Application, $"Failed to delete file {file}: {ex.Message}");
                result.Errors.Add($"Failed to delete {file}: {ex.Message}");
            }
        }

        // Remove empty directories
        var directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
        foreach (var directory in directories.OrderByDescending(d => d.Length))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (Directory.GetFiles(directory).Length == 0 && Directory.GetDirectories(directory).Length == 0)
                {
                    Directory.Delete(directory, recursive: true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(LogCategory.Application, $"Failed to delete directory {directory}: {ex.Message}");
            }
        }
    }

    public async Task UpdateLastCleanupTimeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cleanupMarkerPath = Path.Combine(_settings.RootPath, ".last_cleanup");
            await File.WriteAllTextAsync(cleanupMarkerPath, DateTime.UtcNow.ToString("O"), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(LogCategory.Exception, "Failed to update last cleanup time", ex);
        }
    }
}
