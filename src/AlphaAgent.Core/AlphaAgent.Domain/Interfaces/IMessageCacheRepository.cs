using System;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface IMessageCacheRepository
{
    Task<string?> GetCachedMessagesJsonAsync(Guid conversationId);
    Task<DateTime?> GetLastCachedAtAsync(Guid conversationId);
    Task CacheMessagesJsonAsync(Guid conversationId, string messagesJson);
    Task AppendMessageJsonAsync(Guid conversationId, string messageJson);
    Task ClearCacheAsync(Guid conversationId);
    Task ClearAllCacheAsync();
}