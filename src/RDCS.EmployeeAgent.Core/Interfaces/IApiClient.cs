namespace RDCS.EmployeeAgent.Core.Interfaces;

public interface IApiClient
{
    Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);
    Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);
}
