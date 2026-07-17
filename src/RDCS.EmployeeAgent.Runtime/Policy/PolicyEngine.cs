using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Enums;
using RDCS.EmployeeAgent.Persistence.Repositories;
using RDCS.EmployeeAgent.Runtime.EventBus;
using RDCS.EmployeeAgent.Runtime.EventBus.Events;
using RDCS.EmployeeAgent.Runtime.Policy.Policies;
using RDCS.EmployeeAgent.Runtime.Upload.Policy;
using RDCS.EmployeeAgent.Runtime.Screenshot.Diagnostics;
using System.Collections.Concurrent;

namespace RDCS.EmployeeAgent.Runtime.Policy;

public class PolicyEngine : IPolicyEngine
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IAgentLogger _logger;
    private readonly IEventBus _eventBus;
    private readonly ConcurrentDictionary<string, object> _policyCache = new();

    public PolicyEngine(IPolicyRepository policyRepository, IAgentLogger logger, IEventBus eventBus)
    {
        _policyRepository = policyRepository;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<TPolicy> GetPolicyAsync<TPolicy>(CancellationToken cancellationToken = default) where TPolicy : class
    {
        var policyType = typeof(TPolicy).Name;
        
        // Check cache first
        if (_policyCache.TryGetValue(policyType, out var cachedPolicy))
        {
            ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: Cache HIT for {policyType}");
            return (TPolicy)cachedPolicy;
        }
        
        ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: Cache MISS for {policyType}, loading from repository");
        // Load from SQLite
        var policy = await _policyRepository.GetPolicyAsync<TPolicy>(policyType, cancellationToken);
        
        if (policy == null)
        {
            ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: {policyType} not found in DB, creating and saving default");
            _logger.LogWarning(LogCategory.Application, "Policy {PolicyType} not found in database; creating default", policyType);
            policy = CreateDefaultPolicy<TPolicy>();
            await _policyRepository.SavePolicyAsync(policyType, policy, cancellationToken);
            _policyCache[policyType] = policy;
            return policy;
        }
        
        _policyCache[policyType] = policy;
        ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: {policyType} loaded from DB and cached");
        return policy;
    }

    public async Task<bool> IsPolicyEnabledAsync(string policyType, CancellationToken cancellationToken = default)
    {
        return await _policyRepository.IsPolicyActiveAsync(policyType, cancellationToken);
    }

    public async Task UpdatePolicyAsync<TPolicy>(TPolicy policy, CancellationToken cancellationToken = default) where TPolicy : class
    {
        var policyType = typeof(TPolicy).Name;
        ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: UpdatePolicyAsync called for {policyType}");
        
        await _policyRepository.SavePolicyAsync(policyType, policy, cancellationToken);
        ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: {policyType} saved to repository");
        
        // Update cache
        _policyCache[policyType] = policy;
        ScreenshotWorkerTracer.Trace($"POLICY_ENGINE: {policyType} updated in cache");
        
        // Publish event
        await _eventBus.PublishAsync(new PolicyUpdated(policyType, DateTime.UtcNow), cancellationToken);
        
        _logger.LogInformation(LogCategory.Application, "Policy {PolicyType} updated", policyType);
    }

    public async Task ReloadPoliciesAsync(CancellationToken cancellationToken = default)
    {
        _policyCache.Clear();
        _logger.LogInformation(LogCategory.Application, "Policy cache cleared, policies will be reloaded on next access");
    }

    public async Task<List<PolicyInfo>> GetAllPoliciesAsync(CancellationToken cancellationToken = default)
    {
        // This would require extending the repository to return all policies
        // For now, return empty list
        _logger.LogInformation(LogCategory.Application, "GetAllPolicies called - returning empty list (not fully implemented)");
        return new List<PolicyInfo>();
    }

    private TPolicy CreateDefaultPolicy<TPolicy>() where TPolicy : class
    {
        return typeof(TPolicy).Name switch
        {
            "ScreenshotPolicy" => new ScreenshotPolicy() as TPolicy,
            "BrowserPolicy" => new BrowserPolicy() as TPolicy,
            "ApplicationPolicy" => new ApplicationPolicy() as TPolicy,
            "IdlePolicy" => new IdlePolicy() as TPolicy,
            "UsbPolicy" => new UsbPolicy() as TPolicy,
            "UploadPolicy" => new UploadPolicy() as TPolicy,
            _ => Activator.CreateInstance<TPolicy>()
        };
    }
}
