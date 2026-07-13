namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IFeatureFlagRepository
{
    Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default);
    Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default);
    Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default);
    Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default);
}

public class FeatureFlag
{
    public int Id { get; set; }
    public string FlagName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
    public DateTime DownloadedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
