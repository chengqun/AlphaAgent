using AlphaAgent.Domain.Abstractions.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Chat;

public interface IMessageCacheService
{
    Task<List<ChatMessage>> GetCachedMessagesAsync(Guid conversationId, int? limit = null);
    Task<DateTime?> GetLastCachedAtAsync(Guid conversationId);
    Task CacheMessagesAsync(Guid conversationId, List<ChatMessage> messages, int? maxCacheSize = null);
    Task AppendMessageAsync(Guid conversationId, ChatMessage message);
    Task ClearCacheAsync(Guid conversationId);
}