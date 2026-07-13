namespace RDCS.EmployeeAgent.Runtime.FeatureFlags;

public interface IFeatureFlagManager
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default);
    Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
    Task DownloadFlagsAsync(CancellationToken cancellationToken = default);
    Task ReloadFlagsAsync(CancellationToken cancellationToken = default);
}

public class FeatureFlag
{
    public string FlagName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime DownloadedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
