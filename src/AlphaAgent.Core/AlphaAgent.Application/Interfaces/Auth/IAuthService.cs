using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Auth;

public interface IAuthService
{
    Task SaveTokensAsync(string accessToken, string refreshToken, int? expiresIn, string username, string? password = null, bool rememberMe = false);
    Task<string> GetAccessTokenAsync();
    Task<string?> GetRefreshTokenAsync();
    Task<string?> GetTokenExpirationAsync();
    Task<string?> GetUsernameAsync();
    Task<bool> IsLoggedInAsync();
    Task<bool> IsTokenExpiredAsync();
    Task LogoutAsync();
    Task<List<AccountInfoDto>> GetStoredAccountsAsync();
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password);
    Task<ApiResponse<LoginResponse>> AutoLoginAsync();
    Task<ApiResponse<LoginResponse>> SwitchAccountAsync(string username, string? password = null);
    Task<ApiResponse<bool>> RegisterAsync(string userName, string emailAddress, string password);
    string GetBaseAddress();
}