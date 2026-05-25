using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Domain.Abstractions.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Chat;

public interface IChatService
{
    Task<ApiResponse<Conversation>> GetOrCreateDirectConversationAsync(Guid targetUserId);
    Task<ApiResponse<Conversation>> GetOrCreateGroupConversationAsync(Guid groupId);
    Task<ApiResponse<Conversation>> GetOrCreateDeviceConversationAsync(Guid deviceId);
    Task<ApiResponse<List<Conversation>>> GetMyConversationsAsync();
    Task<ApiResponse<List<ChatMessage>>> GetMessagesAsync(Guid conversationId, int skipCount = 0, int maxResultCount = 50);
    Task<ApiResponse<List<ChatMessage>>> GetUnreadMessagesWithMarkAsync(Guid conversationId);
    Task<ApiResponse<ChatMessage>> SendMessageAsync(Guid conversationId, string content, string messageType = "Text");
    Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId);
    Task<ApiResponse<object>> DeleteConversationAsync(Guid conversationId);
    Task<ApiResponse<int>> GetUnreadCountAsync();
}