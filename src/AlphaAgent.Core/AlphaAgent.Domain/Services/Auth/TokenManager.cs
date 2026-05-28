using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Domain.Services.Auth;

public class TokenManager : ITokenManager
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IHttpClientService _httpClientService;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public TokenManager(ITokenRepository tokenRepository, IHttpClientService httpClientService)
    {
        _tokenRepository = tokenRepository;
        _httpClientService = httpClientService;
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken, int? expiresIn, string username, string? password = null, bool rememberMe = false)
    {
        ValidateTokenData(accessToken, username);

        var token = await _tokenRepository.GetByUsernameAsync(username);

        if (token == null)
        {
            token = CreateNewToken(accessToken, refreshToken, expiresIn, username);
            token.MarkAsLoggedIn();
            await _tokenRepository.AddAsync(token);
        }
        else
        {
            token.Update(accessToken, refreshToken, expiresIn);
            token.MarkAsLoggedIn();
            await _tokenRepository.UpdateAsync(token);
        }

    }

    public async Task<string> GetAccessTokenAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token?.AccessToken ?? string.Empty;
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token?.RefreshToken;
    }

    public async Task<string?> GetTokenExpirationAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token?.TokenExpiration;
    }

    public async Task<string?> GetUsernameAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token?.Username;
    }

    public async Task<bool> IsLoggedInAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token != null && token.IsActive && !token.IsExpired();
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        return token == null || token.IsExpired();
    }

    public async Task<bool> IsTokenAboutToExpireAsync(TimeSpan threshold)
    {
        var token = await _tokenRepository.GetActiveAsync();
        if (token == null)
            return true;

        return token.IsAboutToExpire(threshold);
    }

    public async Task LogoutAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        if (token != null)
        {
            token.MarkAsLoggedOut();
            await _tokenRepository.UpdateAsync(token);
        }
    }

    public async Task<Token?> GetTokenByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        return await _tokenRepository.GetByUsernameAsync(username);
    }

    public async Task<DateTime?> GetActiveTokenExpirationDateTimeAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        if (token == null || string.IsNullOrWhiteSpace(token.TokenExpiration))
            return null;

        return DateTime.Parse(token.TokenExpiration);
    }

    public async Task<bool> TryRefreshTokenAsync()
    {
        await _refreshLock.WaitAsync();
        try
        {
            var refreshToken = await GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
                return false;

            var formData = new
            {
                grant_type = "refresh_token",
                client_id = "alphaagent_chat",
                client_secret = "chat_secret",
                refresh_token = refreshToken
            };

            var response = await _httpClientService.SendAsync<TokenRefreshResponse>("connect/token", formData);
            if (response == null)
                return false;

            var username = await GetUsernameAsync();
            if (string.IsNullOrEmpty(username))
                return false;

            await SaveTokensAsync(response.AccessToken, response.RefreshToken, response.ExpiresIn, username);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private void ValidateTokenData(string accessToken, string username)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new ArgumentNullException(nameof(accessToken));

        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        if (!IsValidUsername(username))
            throw new ArgumentException("无效的用户名格式", nameof(username));
    }

    private bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        return username.Length >= 3 && username.Length <= 100;
    }

    private Token CreateNewToken(string accessToken, string refreshToken, int? expiresIn, string username)
    {
        string expiration = string.Empty;
        if (expiresIn.HasValue && expiresIn.Value > 0)
        {
            expiration = DateTime.UtcNow.AddSeconds(expiresIn.Value).ToString("o");
        }

        return new Token(0, accessToken, refreshToken, expiration, username);
    }

    private class TokenRefreshResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}