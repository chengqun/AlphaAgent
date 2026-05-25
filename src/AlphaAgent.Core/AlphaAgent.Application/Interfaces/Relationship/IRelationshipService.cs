using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Relationship;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Relationship;

public interface IRelationshipService
{
    Task<ApiResponse<RelationshipDto>> CreateRelationshipAsync(int relationshipType, string targetId);
    Task<ApiResponse<RelationshipDto>> AcceptRelationshipAsync(int relationshipType, string relationshipId);
    Task<ApiResponse<RelationshipDto>> RejectRelationshipAsync(int relationshipType, string relationshipId);
    Task<ApiResponse<object>> RemoveRelationshipAsync(int relationshipType, string relationshipId);
    Task<ApiResponse<ContactBookDto>> GetAcceptedContactsAsync();
    Task<ApiResponse<ContactBookDto>> GetPendingRequestsAsync();
    Task<ApiResponse<List<TargetDto>>> SearchAllTargetsAsync(string keyword);
}