using Microsoft.Extensions.Options;

namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StoragePathProvider
{
    private readonly StorageSettings _settings;

    public StoragePathProvider(IOptions<StorageSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GetRootPath() => _settings.RootPath;

    public string GetScreenshotFolder() => CombinePath(_settings.RootPath, _settings.ScreenshotsPath);

    public string GetEmployeeScreenshotFolder(string employeeId, DateTime date)
    {
        return CombinePath(
            _settings.RootPath,
            _settings.ScreenshotsPath,
            date.Year.ToString(),
            date.Month.ToString("D2"),
            date.Day.ToString("D2"),
            employeeId
        );
    }

    public string GetQueueFolder() => CombinePath(_settings.RootPath, _settings.QueuePath);
    public string GetQueuePendingFolder() => CombinePath(_settings.RootPath, _settings.QueuePath, "Pending");
    public string GetQueueProcessingFolder() => CombinePath(_settings.RootPath, _settings.QueuePath, "Processing");
    public string GetQueueFailedFolder() => CombinePath(_settings.RootPath, _settings.QueuePath, "Failed");
    public string GetQueueArchiveFolder() => CombinePath(_settings.RootPath, _settings.QueuePath, "Archive");

    public string GetDatabaseFolder() => CombinePath(_settings.RootPath, _settings.DatabasePath);
    public string GetDatabasePath() => CombinePath(_settings.RootPath, _settings.DatabasePath, "rdcs_agent.db");

    public string GetLogFolder() => CombinePath(_settings.RootPath, _settings.LogsPath);
    public string GetApplicationLogFolder() => CombinePath(_settings.RootPath, _settings.LogsPath, "Application");
    public string GetErrorLogFolder() => CombinePath(_settings.RootPath, _settings.LogsPath, "Error");
    public string GetPerformanceLogFolder() => CombinePath(_settings.RootPath, _settings.LogsPath, "Performance");

    public string GetTempFolder() => CombinePath(_settings.RootPath, _settings.TempPath);
    public string GetTempProcessingFolder() => CombinePath(_settings.RootPath, _settings.TempPath, "Processing");
    public string GetTempUploadsFolder() => CombinePath(_settings.RootPath, _settings.TempPath, "Uploads");

    public string GetCacheFolder() => CombinePath(_settings.RootPath, _settings.CachePath);
    public string GetCachePoliciesFolder() => CombinePath(_settings.RootPath, _settings.CachePath, "Policies");
    public string GetCacheFeatureFlagsFolder() => CombinePath(_settings.RootPath, _settings.CachePath, "FeatureFlags");
    public string GetCacheDownloadsFolder() => CombinePath(_settings.RootPath, _settings.CachePath, "Downloads");
    public string GetCacheMetadataFolder() => CombinePath(_settings.RootPath, _settings.CachePath, "Metadata");

    public string GetConfigFolder() => CombinePath(_settings.RootPath, _settings.ConfigPath);

    public string GetDiagnosticsFolder() => CombinePath(_settings.RootPath, _settings.DiagnosticsPath);
    public string GetCrashDumpsFolder() => CombinePath(_settings.RootPath, _settings.DiagnosticsPath, "CrashDumps");
    public string GetPerformanceReportsFolder() => CombinePath(_settings.RootPath, _settings.DiagnosticsPath, "PerformanceReports");
    public string GetHealthReportsFolder() => CombinePath(_settings.RootPath, _settings.DiagnosticsPath, "HealthReports");

    public string GetBackupFolder() => CombinePath(_settings.RootPath, _settings.BackupPath);
    public string GetDatabaseBackupFolder() => CombinePath(_settings.RootPath, _settings.BackupPath, "Database");
    public string GetConfigBackupFolder() => CombinePath(_settings.RootPath, _settings.BackupPath, "Config");

    public string CombinePath(params string[] segments)
    {
        return Path.Combine(segments);
    }
}
