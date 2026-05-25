using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Chat;

namespace AlphaAgent.Application.Interfaces.Chat;

public interface IConversationSyncService
{
    Task<List<Conversation>> GetCachedConversationsAsync(Guid userId);
    Task<List<Conversation>> SyncFromServerAsync(Guid userId);
    Task UpsertConversationAsync(Conversation conversation, Guid userId);
    Task DeleteConversationAsync(Guid conversationId);
}
