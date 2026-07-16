using RDCS.EmployeeAgent.Core.Interfaces;
using RDCS.EmployeeAgent.Core.Exceptions;
using RDCS.EmployeeAgent.Shared.Results;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RDCS.EmployeeAgent.Infrastructure.Api;

public class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAgentLogger _logger;
    private readonly ITokenStorage _tokenStorage;

    public ApiClient(IHttpClientFactory httpClientFactory, IAgentLogger logger, ITokenStorage tokenStorage)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _tokenStorage = tokenStorage;
    }

    private async Task<HttpClient> GetClientAsync()
    {
        var client = _httpClientFactory.CreateClient("Agent");
        var accessToken = await GetValidAccessTokenAsync();
        if (!string.IsNullOrEmpty(accessToken))
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return client;
    }

    private async Task<string?> GetValidAccessTokenAsync()
    {
        var identity = await _tokenStorage.RetrieveTokensAsync();
        if (identity == null || string.IsNullOrEmpty(identity.AccessToken))
            return null;

        // Access token still valid for at least 2 more minutes — use as-is
        if (identity.ExpiresAt > DateTime.UtcNow.AddMinutes(2))
            return identity.AccessToken;

        // Access token expired or expiring soon — try to refresh
        if (string.IsNullOrEmpty(identity.RefreshToken))
            return identity.AccessToken;

        try
        {
            var rawClient = _httpClientFactory.CreateClient("Agent");
            var response = await rawClient.PostAsJsonAsync(
                "/api/agent/refresh",
                new { refreshToken = identity.RefreshToken });

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(Core.Enums.LogCategory.Authentication, "Token refresh failed with status {Status}", response.StatusCode);
                return identity.AccessToken;
            }

            var json = await response.Content.ReadAsStringAsync();
            var envelope = JsonDocument.Parse(json);
            var data = envelope.RootElement.GetProperty("data");

            var newAccessToken = data.GetProperty("accessToken").GetString() ?? identity.AccessToken;
            var newRefreshToken = data.TryGetProperty("refreshToken", out var rt) ? rt.GetString() ?? identity.RefreshToken : identity.RefreshToken;
            var expiresIn = data.TryGetProperty("expiresIn", out var ei) ? ei.GetInt32() : 3600;

            identity.AccessToken = newAccessToken;
            identity.RefreshToken = newRefreshToken;
            identity.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

            await _tokenStorage.StoreTokensAsync(identity);
            _logger.LogInformation(Core.Enums.LogCategory.Authentication, "Access token refreshed automatically");

            return identity.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(Core.Enums.LogCategory.Authentication, "Automatic token refresh failed", ex);
            return identity.AccessToken;
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    private async Task<TResponse> UnwrapAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var envelope = JsonDocument.Parse(json);

        if (envelope.RootElement.TryGetProperty("data", out var dataElement))
        {
            var result = JsonSerializer.Deserialize<TResponse>(dataElement.GetRawText(), _jsonOptions);
            if (result == null)
                throw new ApiClientException("Failed to deserialize response data");
            return result;
        }

        var direct = JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
        if (direct == null)
            throw new ApiClientException("Failed to deserialize response");
        return direct;
    }

    public async Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(Core.Enums.LogCategory.Network, "POST {Endpoint}", endpoint);

            var client = await GetClientAsync();
            var response = await client.PostAsJsonAsync(endpoint, request, cancellationToken);
            
            await EnsureSuccessAsync(response, cancellationToken);

            return await UnwrapAsync<TResponse>(response, cancellationToken);
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

            var client = await GetClientAsync();
            var response = await client.GetAsync(endpoint, cancellationToken);
            
            await EnsureSuccessAsync(response, cancellationToken);

            return await UnwrapAsync<TResponse>(response, cancellationToken);
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
