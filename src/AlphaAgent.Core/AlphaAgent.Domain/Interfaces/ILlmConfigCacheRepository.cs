using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Interfaces;

public interface ILlmConfigCacheRepository
{
    Task<List<LlmConfigCacheItem>> GetByUserIdAsync(Guid userId);
    Task UpsertRangeAsync(IEnumerable<LlmConfigCacheItem> items);
    Task DeleteByUserIdAsync(Guid userId);
}
