namespace RDCS.EmployeeAgent.Core.Models;

public class AgentSettings
{
    public string ApiUrl { get; set; } = string.Empty;
    public string ApplicationVersion { get; set; } = "1.0.0";
    public string Environment { get; set; } = "production";
    public int RetryCount { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public string LoggingLevel { get; set; } = "Information";
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public FeatureFlags Features { get; set; } = new();
}

public class FeatureFlags
{
    public bool ScreenshotsEnabled { get; set; }
    public bool ApplicationMonitoringEnabled { get; set; }
    public bool WebsiteMonitoringEnabled { get; set; }
    public bool IdleDetectionEnabled { get; set; }
    public bool UsbMonitoringEnabled { get; set; }
}
