using AlphaAgent.Domain.Services.Auth;
using System;
using System.Linq;
using System.Net;
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
            System.Diagnostics.Debug.WriteLine($"[BearerTokenHandler] Path={request.RequestUri?.AbsolutePath}, Token={(!string.IsNullOrEmpty(token) ? token.Substring(0, Math.Min(token.Length, 20)) + "..." : "null/empty")}");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[BearerTokenHandler] Skipped no-auth endpoint: {request.RequestUri?.AbsolutePath}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !IsNoAuthEndpoint(request.RequestUri))
        {
            System.Diagnostics.Debug.WriteLine($"[BearerTokenHandler] 收到 401，尝试刷新 token: {request.RequestUri?.AbsolutePath}");

            var refreshed = await _tokenManager.TryRefreshTokenAsync();
            if (refreshed)
            {
                var newToken = await _tokenManager.GetAccessTokenAsync();
                if (!string.IsNullOrEmpty(newToken))
                {
                    using var retryRequest = await CloneRequestAsync(request);
                    retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                    System.Diagnostics.Debug.WriteLine($"[BearerTokenHandler] Token 刷新成功，重试请求: {request.RequestUri?.AbsolutePath}");
                    return await base.SendAsync(retryRequest, cancellationToken);
                }
            }

            System.Diagnostics.Debug.WriteLine($"[BearerTokenHandler] Token 刷新失败，返回原始 401 响应");
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        if (request.Content != null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            clone.Content = new ByteArrayContent(content);

            foreach (var header in request.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        clone.Version = request.Version;

        return clone;
    }

    private static bool IsNoAuthEndpoint(Uri? requestUri)
    {
        if (requestUri == null) return false;

        var path = requestUri.AbsolutePath.TrimStart('/');
        return NoAuthPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
