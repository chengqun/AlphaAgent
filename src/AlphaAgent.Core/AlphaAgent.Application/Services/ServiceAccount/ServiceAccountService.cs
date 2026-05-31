using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.ServiceAccount;
using AlphaAgent.Application.Interfaces.ServiceAccount;
using AlphaAgent.Domain.Abstractions.Interfaces;

namespace AlphaAgent.Application.Services.ServiceAccount;

/// <summary>
/// 服务号客户端服务
/// </summary>
public class ServiceAccountService : IServiceAccountService
{
    private readonly IHttpClientService _httpClientService;

    public ServiceAccountService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public async Task<ApiResponse<List<ServiceAccountDto>>> SearchAsync(string keyword)
    {
        var response = await _httpClientService.GetAsync<List<ServiceAccountDto>>($"api/app/service-account/search?keyword={Uri.EscapeDataString(keyword)}");
        return response != null
            ? new ApiResponse<List<ServiceAccountDto>> { Success = true, Data = response }
            : new ApiResponse<List<ServiceAccountDto>> { Success = false };
    }

    public async Task<ApiResponse<ServiceAccountDto>> GetAsync(Guid id)
    {
        var response = await _httpClientService.GetAsync<ServiceAccountDto>($"api/app/service-account/{id}");
        return response != null
            ? new ApiResponse<ServiceAccountDto> { Success = true, Data = response }
            : new ApiResponse<ServiceAccountDto> { Success = false };
    }

    public async Task<ApiResponse<List<ServiceAccountDto>>> GetByCategoryAsync(string category)
    {
        var response = await _httpClientService.GetAsync<List<ServiceAccountDto>>($"api/app/service-account/by-category?category={Uri.EscapeDataString(category)}");
        return response != null
            ? new ApiResponse<List<ServiceAccountDto>> { Success = true, Data = response }
            : new ApiResponse<List<ServiceAccountDto>> { Success = false };
    }

    public async Task<ApiResponse<ServiceAccountDto>> FollowAsync(Guid serviceAccountId)
    {
        var response = await _httpClientService.PostAsync<ServiceAccountDto>($"api/app/service-account/follow/{serviceAccountId}", new object());
        return response != null
            ? new ApiResponse<ServiceAccountDto> { Success = true, Data = response }
            : new ApiResponse<ServiceAccountDto> { Success = false };
    }

    public async Task<ApiResponse<object>> UnfollowAsync(Guid serviceAccountId)
    {
        var response = await _httpClientService.PostRawAsync($"api/app/service-account/unfollow/{serviceAccountId}", new object());
        return response != null && response.IsSuccessStatusCode
            ? new ApiResponse<object> { Success = true }
            : new ApiResponse<object> { Success = false, Error = "取消关注失败" };
    }

    public async Task<ApiResponse<List<ServiceAccountDto>>> GetRecommendedAsync()
    {
        var response = await _httpClientService.GetAsync<List<ServiceAccountDto>>("api/app/service-account/all");
        return response != null
            ? new ApiResponse<List<ServiceAccountDto>> { Success = true, Data = response }
            : new ApiResponse<List<ServiceAccountDto>> { Success = false };
    }

    public async Task<ApiResponse<List<ServiceAccountPostListItemDto>>> GetPostsAsync(Guid serviceAccountId, int limit = 50, int offset = 0)
    {
        var response = await _httpClientService.GetAsync<List<ServiceAccountPostListItemDto>>($"api/app/service-account/{serviceAccountId}/posts?limit={limit}&offset={offset}");
        return response != null
            ? new ApiResponse<List<ServiceAccountPostListItemDto>> { Success = true, Data = response }
            : new ApiResponse<List<ServiceAccountPostListItemDto>> { Success = false };
    }

    public async Task<ApiResponse<ServiceAccountPostDto>> GetPostAsync(Guid postId)
    {
        var response = await _httpClientService.GetAsync<ServiceAccountPostDto>($"api/app/service-account/post/{postId}");
        return response != null
            ? new ApiResponse<ServiceAccountPostDto> { Success = true, Data = response }
            : new ApiResponse<ServiceAccountPostDto> { Success = false };
    }

    public async Task<ApiResponse<List<ServiceAccountPostListItemDto>>> GetFollowedPostsAsync(int limit = 50, int offset = 0)
    {
        var response = await _httpClientService.GetAsync<List<ServiceAccountPostListItemDto>>($"api/app/service-account/followed-posts?limit={limit}&offset={offset}");
        return response != null
            ? new ApiResponse<List<ServiceAccountPostListItemDto>> { Success = true, Data = response }
            : new ApiResponse<List<ServiceAccountPostListItemDto>> { Success = false };
    }
}
