using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Auth;
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
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken);
    Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password);
    Task<ApiResponse<LoginResponse>> AutoLoginAsync();
    Task<ApiResponse<bool>> RegisterAsync(string userName, string emailAddress, string password);
    string GetBaseAddress();
}