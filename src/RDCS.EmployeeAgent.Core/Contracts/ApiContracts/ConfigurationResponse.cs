namespace RDCS.EmployeeAgent.Core.Contracts.ApiContracts;

public class ConfigurationResponse
{
    public string ConfigVersion { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = "production";
    public string AgentVersion { get; set; } = "1.0.0";
    public int RetryCount { get; set; } = 3;
    public int TimeoutSeconds { get; set; } = 30;
    public string LoggingLevel { get; set; } = "Information";
    public int HeartbeatIntervalSeconds { get; set; } = 60;
    public FeatureFlagsDto Features { get; set; } = new();
}

public class FeatureFlagsDto
{
    public bool ScreenshotsEnabled { get; set; }
    public bool ApplicationMonitoringEnabled { get; set; }
    public bool WebsiteMonitoringEnabled { get; set; }
    public bool IdleDetectionEnabled { get; set; }
    public bool UsbMonitoringEnabled { get; set; }
}
