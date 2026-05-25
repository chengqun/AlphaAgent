using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Services.Auth;

public interface ITokenManager
{
    Task SaveTokensAsync(string accessToken, string refreshToken, int? expiresIn, string username, string? password = null, bool rememberMe = false);
    Task<string> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<string?> GetTokenExpirationAsync();
    Task<string?> GetUsernameAsync();
    Task<bool> IsLoggedInAsync();
    Task<bool> IsTokenExpiredAsync();
    Task<bool> IsTokenAboutToExpireAsync(TimeSpan threshold);
    Task LogoutAsync();
    Task LogoutAllAsync();
    Task<List<Token>> GetStoredAccountsAsync();
    Task<Token?> GetTokenByUsernameAsync(string username);
    Task SetActiveAsync(string username);
    Task<bool> ExistsAsync(string username);
    Task DeleteTokenAsync(string username);
    Task<int> GetStoredAccountCountAsync();
    Task<DateTime?> GetActiveTokenExpirationDateTimeAsync();
}
