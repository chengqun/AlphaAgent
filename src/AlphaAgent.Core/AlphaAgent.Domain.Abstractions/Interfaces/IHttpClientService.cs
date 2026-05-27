using System.Net.Http;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Abstractions.Interfaces;

public interface IHttpClientService
{
    Task<T?> GetAsync<T>(string endpoint) where T : class;
    Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class;
    Task<T?> DeleteAsync<T>(string endpoint) where T : class;
    Task<HttpResponseMessage?> DeleteRawAsync(string endpoint);
    Task<TResponse?> SendAsync<TResponse>(string endpoint, object? data = null) where TResponse : class;
    Task<HttpResponseMessage?> PostRawAsync(string endpoint, object? data = null);
    Task<byte[]?> GetByteArrayAsync(string endpoint);
    void SetAccessToken(string token);
    void SetAuthorizationToken(string token);
}
