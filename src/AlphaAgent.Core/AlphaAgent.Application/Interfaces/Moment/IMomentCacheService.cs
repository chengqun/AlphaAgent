using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Moment;

namespace AlphaAgent.Application.Interfaces.Moment;

public interface IMomentCacheService
{
    Task<List<MomentDto>> GetCachedMomentsAsync();
    Task<List<MomentDto>> GetCachedMomentsAsync(string targetId);
    Task<DateTime?> GetLatestCachedCreatedAtAsync();
    Task UpdateCacheAsync(List<MomentDto> moments);
    Task AddMomentAsync(MomentDto moment);
    Task ClearCacheAsync();
}