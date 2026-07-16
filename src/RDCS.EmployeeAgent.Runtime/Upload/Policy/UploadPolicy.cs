namespace RDCS.EmployeeAgent.Runtime.Upload.Policy;

public class UploadPolicy
{
    public bool Enabled { get; set; } = true;
    public int IntervalSeconds { get; set; } = 30;
    public int MaxParallelUploads { get; set; } = 3;
    public int MaxRetryCount { get; set; } = 5;
    public int RetryBaseDelayMinutes { get; set; } = 1;
    public bool DeleteLocalAfterUpload { get; set; } = true;
    public bool PauseOnMeteredConnection { get; set; } = true;
    public bool PauseOnLowBattery { get; set; } = true;
    public bool UploadDuringOfficeHours { get; set; } = false;
    public bool CompressionBeforeUpload { get; set; } = false;
    public long MaxUploadBandwidthBytesPerSec { get; set; } = 0;
    public int MaxFileSizeBytes { get; set; } = 52428800;
}
