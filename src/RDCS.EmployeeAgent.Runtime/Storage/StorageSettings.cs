namespace RDCS.EmployeeAgent.Runtime.Storage;

public class StorageSettings
{
    public string Provider { get; set; } = "Local";
    public string RootPath { get; set; } = "C:\\RDCS Agent";
    public string ScreenshotsPath { get; set; } = "Screenshots";
    public string QueuePath { get; set; } = "Queue";
    public string LogsPath { get; set; } = "Logs";
    public string DatabasePath { get; set; } = "Database";
    public string TempPath { get; set; } = "Temp";
    public string CachePath { get; set; } = "Cache";
    public string ConfigPath { get; set; } = "Config";
    public string DiagnosticsPath { get; set; } = "Diagnostics";
    public string BackupPath { get; set; } = "Backups";
    public int MaxLogFileSizeMB { get; set; } = 10;
    public int LogRetentionDays { get; set; } = 30;
    public int TempCleanupIntervalHours { get; set; } = 1;
    public int CacheRetentionDays { get; set; } = 7;
    public int BackupRetentionDays { get; set; } = 30;
    public int TempFileMaxAgeHours { get; set; } = 24;
    public int QueueArchiveRetentionDays { get; set; } = 90;
    public S3Settings S3 { get; set; } = new();
}

public class S3Settings
{
    public string BucketName { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
}
