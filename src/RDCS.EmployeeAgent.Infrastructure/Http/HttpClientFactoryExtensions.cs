using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Polly;
using System.Net.Http.Headers;

namespace RDCS.EmployeeAgent.Infrastructure.Http;

public static class HttpClientFactoryExtensions
{
    public static IHttpClientBuilder AddAgentHttpClient(this IServiceCollection services, string baseUrl)
    {
        return services.AddHttpClient("Agent", client =>
        {
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }
}
