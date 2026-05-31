using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.ServiceAccount;

namespace AlphaAgent.Application.Interfaces.ServiceAccount;

/// <summary>
/// 服务号客户端服务接口
/// </summary>
public interface IServiceAccountService
{
    Task<ApiResponse<List<ServiceAccountDto>>> SearchAsync(string keyword);
    Task<ApiResponse<ServiceAccountDto>> GetAsync(Guid id);
    Task<ApiResponse<List<ServiceAccountDto>>> GetByCategoryAsync(string category);
    Task<ApiResponse<ServiceAccountDto>> FollowAsync(Guid serviceAccountId);
    Task<ApiResponse<object>> UnfollowAsync(Guid serviceAccountId);
    Task<ApiResponse<List<ServiceAccountDto>>> GetRecommendedAsync();
    Task<ApiResponse<List<ServiceAccountPostListItemDto>>> GetPostsAsync(Guid serviceAccountId, int limit = 50, int offset = 0);
    Task<ApiResponse<ServiceAccountPostDto>> GetPostAsync(Guid postId);
    Task<ApiResponse<List<ServiceAccountPostListItemDto>>> GetFollowedPostsAsync(int limit = 50, int offset = 0);
}
