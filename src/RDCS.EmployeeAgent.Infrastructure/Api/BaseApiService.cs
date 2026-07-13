using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Exceptions;

namespace RDCS.EmployeeAgent.Infrastructure.Api;

public abstract class BaseApiService
{
    protected readonly IApiClient _apiClient;
    protected readonly IAgentLogger _logger;

    protected BaseApiService(IApiClient apiClient, IAgentLogger logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    protected async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        int retryCount = 0;
        const int maxRetries = 3;

        while (true)
        {
            try
            {
                return await action();
            }
            catch (NetworkException ex) when (retryCount < maxRetries)
            {
                retryCount++;
                var delay = TimeSpan.FromSeconds(Math.Pow(2, retryCount));
                _logger.LogWarning(Core.Enums.LogCategory.Network, "Network error, retrying {RetryCount}/{MaxRetries} after {Delay}s", retryCount, maxRetries, delay.TotalSeconds);
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
