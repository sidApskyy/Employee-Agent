namespace RDCS.EmployeeAgent.Runtime.Policy;

public interface IPolicyEngine
{
    Task<TPolicy> GetPolicyAsync<TPolicy>(CancellationToken cancellationToken = default) where TPolicy : class;
    Task<bool> IsPolicyEnabledAsync(string policyType, CancellationToken cancellationToken = default);
    Task UpdatePolicyAsync<TPolicy>(TPolicy policy, CancellationToken cancellationToken = default) where TPolicy : class;
    Task ReloadPoliciesAsync(CancellationToken cancellationToken = default);
    Task<List<PolicyInfo>> GetAllPoliciesAsync(CancellationToken cancellationToken = default);
}

public class PolicyInfo
{
    public string PolicyType { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime DownloadedAtUtc { get; set; }
}
