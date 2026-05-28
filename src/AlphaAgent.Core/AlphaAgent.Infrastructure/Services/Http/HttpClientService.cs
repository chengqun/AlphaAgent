using AlphaAgent.Domain.Abstractions.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection;

namespace AlphaAgent.Infrastructure.Services.Http;

public class HttpClientService : IHttpClientService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpClientService> _logger;

    public HttpClientService(HttpClient httpClient, ILogger<HttpClientService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<T?> GetAsync<T>(string endpoint) where T : class
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("GET {Url}", fullUrl);

            var response = await _httpClient.GetAsync(endpoint);
            _logger.LogInformation("GET {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            _logger.LogDebug("GET {Url} - Response: {ResultStatus}", fullUrl, result == null ? "null" : "success");
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "GET request failed: {Message}", ex.Message);
            return default;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "GET request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return default;
        }
    }

    public async Task<T?> PostAsync<T>(string endpoint, object? data = null) where T : class
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("POST {Url}", fullUrl);

            HttpResponseMessage response;
            if (data != null)
            {
                response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            }
            else
            {
                response = await _httpClient.PostAsync(endpoint, null);
            }
            _logger.LogInformation("POST {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);

            response.EnsureSuccessStatusCode();

            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
            {
                _logger.LogDebug("POST {Url} - Response: No Content", fullUrl);
                return default;
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            _logger.LogDebug("POST {Url} - Response: {ResultStatus}", fullUrl, result == null ? "null" : "success");
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "POST request failed: {Message}", ex.Message);
            return default;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "POST request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return default;
        }
    }

    public async Task<T?> DeleteAsync<T>(string endpoint) where T : class
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("DELETE {Url}", fullUrl);

            var response = await _httpClient.DeleteAsync(endpoint);
            _logger.LogInformation("DELETE {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);

            response.EnsureSuccessStatusCode();

            // 处理 204 No Content 或空响应
            if (response.StatusCode == System.Net.HttpStatusCode.NoContent ||
                response.Content.Headers.ContentLength == 0)
            {
                _logger.LogDebug("DELETE {Url} - Response: No Content", fullUrl);
                return default;
            }

            var result = await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
            _logger.LogDebug("DELETE {Url} - Response: {ResultStatus}", fullUrl, result == null ? "null" : "success");
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "DELETE request failed: {Message}", ex.Message);
            return default;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "DELETE request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return default;
        }
    }

    public async Task<HttpResponseMessage?> DeleteRawAsync(string endpoint)
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("DELETE (raw) {Url}", fullUrl);

            var response = await _httpClient.DeleteAsync(endpoint);
            _logger.LogInformation("DELETE (raw) {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DELETE (raw) request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return null;
        }
    }

    public async Task<TResponse?> SendAsync<TResponse>(string endpoint, object? data = null) where TResponse : class
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("POST (form) {Url}", fullUrl);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            if (data != null)
            {
                var formData = ObjectToKeyValuePairs(data);
                request.Content = new FormUrlEncodedContent(formData);
                _logger.LogDebug("Form Field Names: {FieldNames}", string.Join(", ", formData.Select(kv => kv.Key)));
            }
            var response = await _httpClient.SendAsync(request);
            _logger.LogInformation("POST (form) {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
            _logger.LogDebug("POST (form) {Url} - Response: {ResultStatus}", fullUrl, result == null ? "null" : "success");
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "POST (form) request failed: {Message}", ex.Message);
            return default;
        }
        catch (System.Exception ex)
        {
            _logger.LogWarning(ex, "POST (form) request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return default;
        }
    }

    private IEnumerable<KeyValuePair<string, string>> ObjectToKeyValuePairs(object obj)
    {
        var properties = obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var prop in properties)
        {
            var value = prop.GetValue(obj)?.ToString() ?? string.Empty;
            yield return new KeyValuePair<string, string>(prop.Name, value);
        }
    }

    public async Task<byte[]?> GetByteArrayAsync(string endpoint)
    {
        try
        {
            return await _httpClient.GetByteArrayAsync(endpoint);
        }
        catch
        {
            return default;
        }
    }

    public async Task<HttpResponseMessage?> PostRawAsync(string endpoint, object? data = null)
    {
        try
        {
            var fullUrl = $"{_httpClient.BaseAddress}{endpoint}";
            _logger.LogInformation("POST (raw) {Url}", fullUrl);

            HttpResponseMessage response;
            if (data != null)
            {
                response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);
            }
            else
            {
                response = await _httpClient.PostAsync(endpoint, null);
            }
            _logger.LogInformation("POST (raw) {Url} - Status: {StatusCode} ({StatusCodeValue})", fullUrl, response.StatusCode, (int)response.StatusCode);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "POST (raw) request exception: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
            return null;
        }
    }
}
