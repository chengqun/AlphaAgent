using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Domain.Services.Auth;

public class TokenManager : ITokenManager
{
    private readonly ITokenRepository _tokenRepository;
    private const int MaxStoredAccounts = 10;

    public TokenManager(ITokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken, int? expiresIn, string username, string? password = null, bool rememberMe = false)
    {
        ValidateTokenData(accessToken, username);

        var existingTokens = await _tokenRepository.GetAllAsync();
        if (existingTokens.Count >= MaxStoredAccounts && !await ExistsAsync(username))
        {
            await RemoveOldestInactiveTokenAsync();
        }

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

        await _tokenRepository.SetActiveAsync(username);
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

    public async Task LogoutAllAsync()
    {
        var tokens = await _tokenRepository.GetAllAsync();
        foreach (var token in tokens)
        {
            token.MarkAsLoggedOut();
            await _tokenRepository.UpdateAsync(token);
        }
    }

    public async Task<List<Token>> GetStoredAccountsAsync()
    {
        var tokens = await _tokenRepository.GetAllAsync();
        return tokens.OrderByDescending(t => t.IsActive).ThenByDescending(t => t.UpdatedAt).ToList();
    }

    public async Task<Token?> GetTokenByUsernameAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        return await _tokenRepository.GetByUsernameAsync(username);
    }

    public async Task SetActiveAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        await _tokenRepository.SetActiveAsync(username);
    }

    public async Task<bool> ExistsAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        return await _tokenRepository.ExistsAsync(username);
    }

    public async Task DeleteTokenAsync(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentNullException(nameof(username));

        var token = await _tokenRepository.GetByUsernameAsync(username);
        if (token == null)
            throw new InvalidOperationException($"未找到用户 {username} 的令牌");

        await _tokenRepository.DeleteAsync(token.Id);
    }

    public async Task<int> GetStoredAccountCountAsync()
    {
        var tokens = await _tokenRepository.GetAllAsync();
        return tokens.Count;
    }

    public async Task<DateTime?> GetActiveTokenExpirationDateTimeAsync()
    {
        var token = await _tokenRepository.GetActiveAsync();
        if (token == null || string.IsNullOrWhiteSpace(token.TokenExpiration))
            return null;

        return DateTime.Parse(token.TokenExpiration);
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

    private async Task RemoveOldestInactiveTokenAsync()
    {
        var tokens = await _tokenRepository.GetAllAsync();
        var oldestInactive = tokens
            .Where(t => !t.IsActive)
            .OrderBy(t => t.UpdatedAt)
            .FirstOrDefault();

        if (oldestInactive != null)
        {
            await _tokenRepository.DeleteAsync(oldestInactive.Id);
        }
    }
}