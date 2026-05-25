using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Moment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Moment;

public interface IMomentService
{
    Task<ApiResponse<List<MomentDto>>> GetFriendsMomentsAsync(int limit = 50, int offset = 0, DateTime? since = null);
    Task<ApiResponse<List<MomentDto>>> GetMomentsAsync(string targetId, string type, int limit = 50, int offset = 0, DateTime? since = null);
    Task<ApiResponse<MomentDto>> CreateMomentAsync(CreateMomentDto input);
    Task<ApiResponse<object>> DeleteMomentAsync(string momentId);
}