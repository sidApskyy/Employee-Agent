using RDCS.EmployeeAgent.Core.Interfaces;
using System.Net.Http.Headers;

namespace RDCS.EmployeeAgent.Infrastructure.Api;

public class AuthenticationHeaderHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;

    public AuthenticationHeaderHandler(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var identity = await _tokenStorage.RetrieveTokensAsync(cancellationToken);
        
        if (identity != null && !string.IsNullOrEmpty(identity.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", identity.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
