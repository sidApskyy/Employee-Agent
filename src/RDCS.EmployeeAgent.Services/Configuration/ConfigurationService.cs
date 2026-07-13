using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Models;
using RDCS.EmployeeAgent.Core.Contracts.ApiContracts;
using RDCS.EmployeeAgent.Infrastructure.Api;
using RDCS.EmployeeAgent.Shared.Constants;
using System.Text.Json;

namespace RDCS.EmployeeAgent.Services.Configuration;

public class ConfigurationService : BaseApiService, IConfigurationService
{
    private readonly string _settingsFilePath;

    public ConfigurationService(
        IApiClient apiClient,
        IAgentLogger logger)
        : base(apiClient, logger)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configFolder = Path.Combine(localAppData, "RDCS", "EmployeeAgent");
        
        if (!Directory.Exists(configFolder))
        {
            Directory.CreateDirectory(configFolder);
        }

        _settingsFilePath = Path.Combine(configFolder, ApplicationConstants.SettingsFileName);
    }

    public async Task<ConfigurationManifest> DownloadConfigurationAsync(string version = "current", CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Configuration, "Downloading configuration version {Version}", version);

        // Development mode: bypass API for testing
        #if DEBUG
        _logger.LogWarning(Core.Enums.LogCategory.Configuration, "Development mode: using default configuration");
        
        var manifest = new ConfigurationManifest
        {
            ConfigVersion = "1.0.0",
            ApiUrl = "https://api.rdcs.example.com",
            Environment = "development",
            AgentVersion = "1.0.0",
            RetryCount = 3,
            TimeoutSeconds = 30,
            LoggingLevel = "Information",
            HeartbeatIntervalSeconds = 60,
            Features = new FeatureFlags
            {
                ScreenshotsEnabled = true,
                ApplicationMonitoringEnabled = true,
                WebsiteMonitoringEnabled = true,
                IdleDetectionEnabled = true,
                UsbMonitoringEnabled = true
            }
        };

        _logger.LogInformation(Core.Enums.LogCategory.Configuration, "Configuration loaded successfully");
        return manifest;
        #else
        var query = $"?version={version}&environment=production";
        var response = await _apiClient.GetAsync<ConfigurationResponse>(ApiRoutes.Config + query, cancellationToken);

        var manifest = new ConfigurationManifest
        {
            ConfigVersion = response.ConfigVersion,
            ApiUrl = response.ApiUrl,
            Environment = response.Environment,
            AgentVersion = response.AgentVersion,
            RetryCount = response.RetryCount,
            TimeoutSeconds = response.TimeoutSeconds,
            LoggingLevel = response.LoggingLevel,
            HeartbeatIntervalSeconds = response.HeartbeatIntervalSeconds,
            Features = new FeatureFlags
            {
                ScreenshotsEnabled = response.Features.ScreenshotsEnabled,
                ApplicationMonitoringEnabled = response.Features.ApplicationMonitoringEnabled,
                WebsiteMonitoringEnabled = response.Features.WebsiteMonitoringEnabled,
                IdleDetectionEnabled = response.Features.IdleDetectionEnabled,
                UsbMonitoringEnabled = response.Features.UsbMonitoringEnabled
            }
        };

        _logger.LogInformation(Core.Enums.LogCategory.Configuration, "Configuration downloaded successfully");

        return manifest;
        #endif
    }

    public async Task ApplyConfigurationAsync(ConfigurationManifest configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(Core.Enums.LogCategory.Configuration, "Applying configuration version {ConfigVersion}", configuration.ConfigVersion);

        var settings = new AgentSettings
        {
            ApiUrl = configuration.ApiUrl,
            ApplicationVersion = configuration.AgentVersion,
            Environment = configuration.Environment,
            RetryCount = configuration.RetryCount,
            TimeoutSeconds = configuration.TimeoutSeconds,
            LoggingLevel = configuration.LoggingLevel,
            HeartbeatIntervalSeconds = configuration.HeartbeatIntervalSeconds,
            Features = configuration.Features
        };

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(_settingsFilePath, json, cancellationToken);

        _logger.LogInformation(Core.Enums.LogCategory.Configuration, "Configuration applied successfully");
    }

    public async Task<AgentSettings> GetLocalSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFilePath))
        {
            _logger.LogWarning(Core.Enums.LogCategory.Configuration, "Local settings file not found, returning default settings");
            return GetDefaultSettings();
        }

        var json = await File.ReadAllTextAsync(_settingsFilePath, cancellationToken);
        var settings = JsonSerializer.Deserialize<AgentSettings>(json);

        return settings ?? GetDefaultSettings();
    }

    private static AgentSettings GetDefaultSettings()
    {
        return new AgentSettings
        {
            ApiUrl = "https://api.rdcs.example.com",
            ApplicationVersion = "1.0.0",
            Environment = "production",
            RetryCount = 3,
            TimeoutSeconds = 30,
            LoggingLevel = "Information",
            HeartbeatIntervalSeconds = 60,
            Features = new FeatureFlags()
        };
    }
}
