namespace RDCS.EmployeeAgent.Runtime.Policy.Policies;

public class ScreenshotPolicy
{
    public bool Enabled { get; set; }
    public int IntervalSeconds { get; set; } = 300;
    public int Quality { get; set; } = 85;
    public string Format { get; set; } = "JPEG";
    public bool CaptureActiveWindowOnly { get; set; } = false;
    public bool CaptureOnIdle { get; set; } = false;
    public int IdleThresholdSeconds { get; set; } = 300;
    public bool CaptureDuringOfficeHours { get; set; } = true;
    public TimeSpan OfficeHoursStart { get; set; } = TimeSpan.FromHours(9);
    public TimeSpan OfficeHoursEnd { get; set; } = TimeSpan.FromHours(17);
    public bool CaptureMultiMonitor { get; set; } = true;
    public bool CompressionEnabled { get; set; } = true;
    public int MaxWidth { get; set; } = 1920;
    public int MaxHeight { get; set; } = 1080;
    public bool LocalStorageEnabled { get; set; } = true;
    public long MaximumLocalStorageSizeBytes { get; set; } = 10737418240;
    public int AutoCleanupDays { get; set; } = 30;
}
