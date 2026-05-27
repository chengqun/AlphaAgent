using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Domain.Services.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Relationship;

public class RelationshipService : IRelationshipService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ITokenManager _tokenManager;

    public RelationshipService(IHttpClientService httpClientService, ITokenManager tokenManager)
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

    public async Task<ApiResponse<RelationshipDto>> CreateRelationshipAsync(int relationshipType, string targetId)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.PostAsync<RelationshipDto>($"api/app/relationship/relationship/{targetId}?type={relationshipType}", new object());
        return response != null
            ? new ApiResponse<RelationshipDto> { Success = true, Data = response }
            : new ApiResponse<RelationshipDto> { Success = false };
    }

    public async Task<ApiResponse<RelationshipDto>> AcceptRelationshipAsync(int relationshipType, string relationshipId)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.PostAsync<RelationshipDto>($"api/app/relationship/accept-relationship/{relationshipId}?type={relationshipType}", new object());
        return response != null
            ? new ApiResponse<RelationshipDto> { Success = true, Data = response }
            : new ApiResponse<RelationshipDto> { Success = false };
    }

    public async Task<ApiResponse<RelationshipDto>> RejectRelationshipAsync(int relationshipType, string relationshipId)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.PostAsync<RelationshipDto>($"api/app/relationship/reject-relationship/{relationshipId}?type={relationshipType}", new object());
        return response != null
            ? new ApiResponse<RelationshipDto> { Success = true, Data = response }
            : new ApiResponse<RelationshipDto> { Success = false };
    }

    public async Task<ApiResponse<object>> RemoveRelationshipAsync(int relationshipType, string relationshipId)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.DeleteRawAsync($"api/app/relationship/relationship/{relationshipId}?type={relationshipType}");
        return response != null && response.IsSuccessStatusCode
            ? new ApiResponse<object> { Success = true }
            : new ApiResponse<object> { Success = false, Error = "删除失败" };
    }

    public async Task<ApiResponse<ContactBookDto>> GetAcceptedContactsAsync()
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.GetAsync<ContactBookDto>("api/app/relationship/accepted-contacts");
        return response != null
            ? new ApiResponse<ContactBookDto> { Success = true, Data = response }
            : new ApiResponse<ContactBookDto> { Success = false };
    }

    public async Task<ApiResponse<ContactBookDto>> GetPendingRequestsAsync()
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.GetAsync<ContactBookDto>("api/app/relationship/pending-requests");
        return response != null
            ? new ApiResponse<ContactBookDto> { Success = true, Data = response }
            : new ApiResponse<ContactBookDto> { Success = false };
    }

    public async Task<ApiResponse<List<TargetDto>>> SearchAllTargetsAsync(string keyword)
    {
        await EnsureTokenAsync();
        var response = await _httpClientService.GetAsync<List<TargetDto>>($"api/app/relationship/search-all-targets?keyword={keyword}");
        return response != null
            ? new ApiResponse<List<TargetDto>> { Success = true, Data = response }
            : new ApiResponse<List<TargetDto>> { Success = false };
    }
}