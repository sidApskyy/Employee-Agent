namespace RDCS.EmployeeAgent.Persistence.Repositories;

public interface IPolicyRepository
{
    Task<TPolicy?> GetPolicyAsync<TPolicy>(string policyType, CancellationToken cancellationToken = default) where TPolicy : class;
    Task SavePolicyAsync<TPolicy>(string policyType, TPolicy policy, CancellationToken cancellationToken = default) where TPolicy : class;
    Task<bool> IsPolicyActiveAsync(string policyType, CancellationToken cancellationToken = default);
}
