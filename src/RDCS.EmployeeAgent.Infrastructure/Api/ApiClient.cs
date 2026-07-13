using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Exceptions;
using RDCS.EmployeeAgent.Shared.Results;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RDCS.EmployeeAgent.Infrastructure.Api;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAgentLogger _logger;

    public ApiClient(HttpClient httpClient, IAgentLogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(Core.Enums.LogCategory.Network, "POST {Endpoint}", endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);
            
            await EnsureSuccessAsync(response, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                throw new ApiClientException($"Failed to deserialize response from {endpoint}");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Network, "POST {Endpoint} failed", ex, endpoint);
            throw new NetworkException("Network request failed", ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning(Core.Enums.LogCategory.Network, "POST {Endpoint} was cancelled", endpoint);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Network, "POST {Endpoint} timed out", ex, endpoint);
            throw new NetworkException("Request timed out", ex);
        }
    }

    public async Task<TResponse> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(Core.Enums.LogCategory.Network, "GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            
            await EnsureSuccessAsync(response, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            
            if (result == null)
            {
                throw new ApiClientException($"Failed to deserialize response from {endpoint}");
            }

            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Network, "GET {Endpoint} failed", ex, endpoint);
            throw new NetworkException("Network request failed", ex);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            _logger.LogWarning(Core.Enums.LogCategory.Network, "GET {Endpoint} was cancelled", endpoint);
            throw;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Network, "GET {Endpoint} timed out", ex, endpoint);
            throw new NetworkException("Request timed out", ex);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(Core.Enums.LogCategory.Network, $"HTTP request failed with status {response.StatusCode}: {content}");
            throw new ApiClientException($"HTTP request failed with status {response.StatusCode}: {content}");
        }
    }
}
