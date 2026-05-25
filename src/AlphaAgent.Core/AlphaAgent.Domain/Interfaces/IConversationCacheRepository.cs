using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Domain.Interfaces;

public interface IConversationCacheRepository
{
    Task<List<ConversationCacheItem>> GetAllAsync(Guid userId);
    Task UpsertAsync(ConversationCacheItem item);
    Task UpsertRangeAsync(IEnumerable<ConversationCacheItem> items);
    Task DeleteAsync(Guid conversationId);
    Task DeleteAllAsync(Guid userId);
    Task<DateTime?> GetLastCachedAtAsync(Guid userId);
}
