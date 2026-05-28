using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Relationship;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Relationship;

public class GroupService : IGroupService
{
    private readonly IHttpClientService _httpClientService;

    public GroupService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public async Task<ApiResponse<List<GroupDto>>> GetMyGroupsAsync()
    {
        var response = await _httpClientService.GetAsync<List<GroupDto>>("api/app/group/my-groups");
        return response != null
            ? new ApiResponse<List<GroupDto>> { Success = true, Data = response }
            : new ApiResponse<List<GroupDto>> { Success = false };
    }

    public async Task<ApiResponse<List<GroupDto>>> SearchGroupsAsync(string keyword)
    {
        var response = await _httpClientService.GetAsync<List<GroupDto>>($"api/app/group/search-groups?keyword={keyword}");
        return response != null
            ? new ApiResponse<List<GroupDto>> { Success = true, Data = response }
            : new ApiResponse<List<GroupDto>> { Success = false };
    }

    public async Task<ApiResponse<List<GroupMemberDto>>> GetGroupMembersAsync(string groupId)
    {
        var response = await _httpClientService.GetAsync<List<GroupMemberDto>>($"api/app/group/group-members/{groupId}");
        return response != null
            ? new ApiResponse<List<GroupMemberDto>> { Success = true, Data = response }
            : new ApiResponse<List<GroupMemberDto>> { Success = false };
    }

    public async Task<ApiResponse<object>> AddMemberAsync(string groupId, string username)
    {
        var response = await _httpClientService.PostAsync<object>($"api/app/group/member/{groupId}", new { Username = username });
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }

    public async Task<ApiResponse<object>> RemoveMemberAsync(string groupId, string userId)
    {
        var response = await _httpClientService.DeleteAsync<object>($"api/app/group/member?groupId={groupId}&userId={userId}");
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }

    public async Task<ApiResponse<object>> DisbandGroupAsync(string groupId)
    {
        var response = await _httpClientService.PostAsync<object>($"api/app/group/disband-group/{groupId}", new { });
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }
}