using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Interfaces;

public interface IContactCacheRepository
{
    Task<List<ContactCacheItem>> GetAllAsync(Guid userId);
    Task UpsertRangeAsync(IEnumerable<ContactCacheItem> items);
    Task DeleteAllAsync(Guid userId);
    Task<DateTime?> GetLastCachedAtAsync(Guid userId);
}
