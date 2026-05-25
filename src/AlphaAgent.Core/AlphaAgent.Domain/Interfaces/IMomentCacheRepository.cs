using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Interfaces;

public interface IMomentCacheRepository
{
    Task<List<MomentCacheItem>> GetAllAsync();
    Task<DateTime?> GetLatestCreatedAtAsync();
    Task AddOrUpdateAsync(IEnumerable<MomentCacheItem> items);
    Task AddAsync(MomentCacheItem item);
    Task ClearAllAsync();
}