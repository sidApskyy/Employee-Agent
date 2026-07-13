using RDCS.EmployeeAgent.Core.Models;

namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IConfigurationService
{
    Task<ConfigurationManifest> DownloadConfigurationAsync(string version = "current", CancellationToken cancellationToken = default);
    Task ApplyConfigurationAsync(ConfigurationManifest configuration, CancellationToken cancellationToken = default);
    Task<AgentSettings> GetLocalSettingsAsync(CancellationToken cancellationToken = default);
}
