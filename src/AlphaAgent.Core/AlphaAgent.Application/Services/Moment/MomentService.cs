using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Moment;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Moment;
using AlphaAgent.Domain.Services.Auth;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Moment;

public class MomentService : IMomentService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ITokenManager _tokenManager;

    public MomentService(IHttpClientService httpClientService, ITokenManager tokenManager)
    {
        _httpClientService = httpClientService;
        _tokenManager = tokenManager;
    }

    private async Task EnsureTokenAsync()
    {
        var token = await _tokenManager.GetTokenByUsernameAsync(await _tokenManager.GetUsernameAsync() ?? string.Empty);
        if (token != null && !token.IsExpired())
        {
            _httpClientService.SetAuthorizationToken(token.AccessToken);
        }
    }

    public async Task<ApiResponse<List<MomentDto>>> GetFriendsMomentsAsync(int limit = 50, int offset = 0, DateTime? since = null)
    {
        await EnsureTokenAsync();
        var url = $"api/app/moment/friends-moments?limit={limit}&offset={offset}";
        if (since.HasValue)
            url += $"&since={since.Value:O}";
        var response = await _httpClientService.GetAsync<List<MomentDto>>(url);
        return response != null
            ? new ApiResponse<List<MomentDto>> { Success = true, Data = response }
            : new ApiResponse<List<MomentDto>> { Success = false };
    }

    public async Task<ApiResponse<List<MomentDto>>> GetMomentsAsync(string targetId, string type, int limit = 50, int offset = 0, DateTime? since = null)
    {
        await EnsureTokenAsync();
        var url = $"api/app/moment/moments/{targetId}?type={type}&limit={limit}&offset={offset}";
        if (since.HasValue)
            url += $"&since={since.Value:O}";
        var response = await _httpClientService.GetAsync<List<MomentDto>>(url);
        return response != null
            ? new ApiResponse<List<MomentDto>> { Success = true, Data = response }
            : new ApiResponse<List<MomentDto>> { Success = false };
    }

    public async Task<ApiResponse<MomentDto>> CreateMomentAsync(CreateMomentDto input)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.PostAsync<MomentDto>("api/app/moment", input);
        return response != null
            ? new ApiResponse<MomentDto> { Success = true, Data = response }
            : new ApiResponse<MomentDto> { Success = false };
    }

    public async Task<ApiResponse<object>> DeleteMomentAsync(string momentId)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.DeleteAsync<object>($"api/app/moment/{momentId}");
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }
}