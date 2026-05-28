using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Auth;
using AlphaAgent.Domain.Services.Auth;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly ITokenManager _tokenManager;
    private readonly IHttpClientService _httpClientService;

    public AuthService(ITokenManager tokenManager, IHttpClientService httpClientService)
    {
        _tokenManager = tokenManager;
        _httpClientService = httpClientService;
    }

    public async Task SaveTokensAsync(string accessToken, string refreshToken, int? expiresIn, string username, string? password = null, bool rememberMe = false)
    {
        await _tokenManager.SaveTokensAsync(accessToken, refreshToken, expiresIn, username, password, rememberMe);
    }

    public async Task<string> GetAccessTokenAsync()
    {
        return await _tokenManager.GetAccessTokenAsync();
    }

    public async Task<string?> GetRefreshTokenAsync()
    {
        return await _tokenManager.GetRefreshTokenAsync();
    }

    public async Task<string?> GetTokenExpirationAsync()
    {
        return await _tokenManager.GetTokenExpirationAsync();
    }

    public async Task<string?> GetUsernameAsync()
    {
        return await _tokenManager.GetUsernameAsync();
    }

    public async Task<bool> IsLoggedInAsync()
    {
        return await _tokenManager.IsLoggedInAsync();
    }

    public async Task<bool> IsTokenExpiredAsync()
    {
        return await _tokenManager.IsTokenExpiredAsync();
    }

    public async Task LogoutAsync()
    {
        await _tokenManager.LogoutAsync();
    }

    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(string refreshToken)
    {
        var formData = new
        {
            grant_type = "refresh_token",
            client_id = "alphaagent_chat",
            client_secret = "chat_secret",
            refresh_token = refreshToken
        };

        var response = await _httpClientService.SendAsync<LoginResponse>("connect/token", formData);

        if (response != null)
        {
            var username = await _tokenManager.GetUsernameAsync();
            if (!string.IsNullOrEmpty(username))
            {
                await _tokenManager.SaveTokensAsync(
                    response.AccessToken,
                    response.RefreshToken,
                    response.ExpiresIn,
                    username);
            }
            return new ApiResponse<LoginResponse> { Success = true, Data = response };
        }

        return new ApiResponse<LoginResponse> { Success = false, Error = "刷新令牌失败" };
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(string username, string password)
    {
        var formData = new
        {
            grant_type = "password",
            client_id = "alphaagent_chat",
            client_secret = "chat_secret",
            username = username,
            password = password,
            scope = "Abp alphaagent_chat"
        };

        var response = await _httpClientService.SendAsync<LoginResponse>("connect/token", formData);

        if (response != null)
        {
            await _tokenManager.SaveTokensAsync(
                response.AccessToken,
                response.RefreshToken,
                response.ExpiresIn,
                username,
                password);
            return new ApiResponse<LoginResponse> { Success = true, Data = response };
        }

        return new ApiResponse<LoginResponse> { Success = false, Error = "登录失败" };
    }

    public async Task<ApiResponse<LoginResponse>> AutoLoginAsync()
    {
        var token = await _tokenManager.GetTokenByUsernameAsync(await _tokenManager.GetUsernameAsync() ?? string.Empty);
        if (token == null)
        {
            return new ApiResponse<LoginResponse> { Success = false, Error = "没有找到活跃的用户" };
        }

        if (!token.IsExpired())
        {
            return new ApiResponse<LoginResponse>
            {
                Success = true,
                Data = new LoginResponse
                {
                    AccessToken = token.AccessToken,
                    RefreshToken = token.RefreshToken
                }
            };
        }

        if (!string.IsNullOrEmpty(token.RefreshToken))
        {
            return await RefreshTokenAsync(token.RefreshToken);
        }

        return new ApiResponse<LoginResponse> { Success = false, Error = "登录状态已过期" };
    }

    public string GetBaseAddress()
    {
        return string.Empty;
    }

    public async Task<ApiResponse<bool>> RegisterAsync(string userName, string emailAddress, string password)
    {
        var request = new RegisterRequest
        {
            UserName = userName,
            EmailAddress = emailAddress,
            Password = password
        };

        var response = await _httpClientService.PostRawAsync("api/account/register", request);

        if (response == null)
        {
            return new ApiResponse<bool> { Success = false, Error = "注册服务不可用" };
        }

        if (response.IsSuccessStatusCode)
        {
            return new ApiResponse<bool> { Success = true, Data = true };
        }

        // 解析 ABP 错误响应
        try
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(errorBody))
            {
                var errorResponse = JsonSerializer.Deserialize<AbpErrorResponse>(errorBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var errorMessage = errorResponse?.Error?.Message;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    return new ApiResponse<bool> { Success = false, Error = errorMessage };
                }
            }
        }
        catch
        {
            // 忽略解析错误，使用默认消息
        }

        return new ApiResponse<bool> { Success = false, Error = "注册失败" };
    }
}