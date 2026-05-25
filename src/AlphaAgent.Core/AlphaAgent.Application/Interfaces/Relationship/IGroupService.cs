using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Relationship;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Relationship;

public interface IGroupService
{
    Task<ApiResponse<List<GroupDto>>> GetMyGroupsAsync();
    Task<ApiResponse<List<GroupDto>>> SearchGroupsAsync(string keyword);
    Task<ApiResponse<List<GroupMemberDto>>> GetGroupMembersAsync(string groupId);
    Task<ApiResponse<object>> AddMemberAsync(string groupId, string username);
    Task<ApiResponse<object>> RemoveMemberAsync(string groupId, string userId);
    Task<ApiResponse<object>> DisbandGroupAsync(string groupId);
}