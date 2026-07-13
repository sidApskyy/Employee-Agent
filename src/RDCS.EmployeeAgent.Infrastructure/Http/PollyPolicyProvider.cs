using Polly;
using Polly.Retry;
using Polly.Timeout;

namespace RDCS.EmployeeAgent.Infrastructure.Http;

public static class PollyPolicyProvider
{
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(int retryCount = 3)
    {
        return Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .WaitAndRetryAsync(
                retryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempt here if needed
                });
    }

    public static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy(TimeSpan timeout)
    {
        return Policy
            .TimeoutAsync<HttpResponseMessage>(timeout);
    }
}
