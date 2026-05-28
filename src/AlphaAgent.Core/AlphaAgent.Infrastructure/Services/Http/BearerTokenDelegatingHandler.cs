using AlphaAgent.Domain.Services.Auth;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Http;

public class BearerTokenDelegatingHandler : DelegatingHandler
{
    private readonly ITokenManager _tokenManager;

    private static readonly string[] NoAuthPathPrefixes =
    {
        "connect/",
        "api/account/register"
    };

    public BearerTokenDelegatingHandler(ITokenManager tokenManager)
    {
        _tokenManager = tokenManager;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!IsNoAuthEndpoint(request.RequestUri))
        {
            var token = await _tokenManager.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsNoAuthEndpoint(Uri? requestUri)
    {
        if (requestUri == null) return false;

        var path = requestUri.AbsolutePath.TrimStart('/');
        return NoAuthPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
