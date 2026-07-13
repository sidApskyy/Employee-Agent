using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;
using System.Collections.Concurrent;

namespace RDCS.EmployeeAgent.Runtime.FeatureFlags;

public class FeatureFlagManager : IFeatureFlagManager
{
    private readonly IFeatureFlagRepository _repository;
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, bool> _flagCache = new();

    public FeatureFlagManager(IFeatureFlagRepository repository, IAgentLogger logger, IEventBus eventBus)
    {
        _repository = repository;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<bool> IsEnabledAsync(string flagName, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_flagCache.TryGetValue(flagName, out var isEnabled))
        {
            return isEnabled;
        }
        
        // Load from repository
        var flag = await _repository.GetFlagAsync(flagName, cancellationToken);
        
        if (flag == null)
        {
            _logger.LogWarning(LogCategory.Application, "Feature flag {FlagName} not found, defaulting to false", flagName);
            _flagCache[flagName] = false;
            return false;
        }
        
        _flagCache[flagName] = flag.IsEnabled;
        return flag.IsEnabled;
    }

    public async Task SetFlagAsync(string flagName, bool isEnabled, CancellationToken cancellationToken = default)
    {
        await _repository.SetFlagAsync(flagName, isEnabled, cancellationToken);
        
        // Update cache
        _flagCache[flagName] = isEnabled;
        
        _logger.LogInformation(LogCategory.Application, "Feature flag {FlagName} set to {IsEnabled}", flagName, isEnabled);
    }

    public async Task<FeatureFlag?> GetFlagAsync(string flagName, CancellationToken cancellationToken = default)
    {
        var persistenceFlag = await _repository.GetFlagAsync(flagName, cancellationToken);
        if (persistenceFlag == null) return null;

        return new FeatureFlag
        {
            FlagName = persistenceFlag.FlagName,
            IsEnabled = persistenceFlag.IsEnabled,
            Description = persistenceFlag.Description,
            DownloadedAtUtc = persistenceFlag.DownloadedAtUtc,
            UpdatedAtUtc = persistenceFlag.UpdatedAtUtc
        };
    }

    public async Task<List<FeatureFlag>> GetAllFlagsAsync(CancellationToken cancellationToken = default)
    {
        var persistenceFlags = await _repository.GetAllFlagsAsync(cancellationToken);
        return persistenceFlags.Select(pf => new FeatureFlag
        {
            FlagName = pf.FlagName,
            IsEnabled = pf.IsEnabled,
            Description = pf.Description,
            DownloadedAtUtc = pf.DownloadedAtUtc,
            UpdatedAtUtc = pf.UpdatedAtUtc
        }).ToList();
    }

    public async Task DownloadFlagsAsync(CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would download flags from the backend API
        // For now, we'll log that this would happen
        _logger.LogInformation(LogCategory.Application, "Feature flags download from backend not yet implemented");
        
        // Simulate downloading by reloading from repository
        await ReloadFlagsAsync(cancellationToken);
    }

    public async Task ReloadFlagsAsync(CancellationToken cancellationToken = default)
    {
        _flagCache.Clear();
        
        var flags = await _repository.GetAllFlagsAsync(cancellationToken);
        
        foreach (var flag in flags)
        {
            _flagCache[flag.FlagName] = flag.IsEnabled;
        }
        
        _logger.LogInformation(LogCategory.Application, "Feature flags reloaded, {Count} flags loaded", flags.Count);
        
        // Publish event
        await _eventBus.PublishAsync(new ConfigurationChanged("FeatureFlags", DateTime.UtcNow), cancellationToken);
    }
}
