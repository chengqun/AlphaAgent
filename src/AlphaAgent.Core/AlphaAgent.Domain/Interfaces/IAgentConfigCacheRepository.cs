using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Interfaces;

public interface IAgentConfigCacheRepository
{
    Task<List<AgentConfigCacheItem>> GetByUserIdAsync(Guid userId);
    Task UpsertRangeAsync(IEnumerable<AgentConfigCacheItem> items);
    Task DeleteByUserIdAsync(Guid userId);
}