using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Domain.Abstractions.Chat;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Chat;

public class ChatService : IChatService
{
    private readonly IHttpClientService _httpClientService;

    public ChatService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public async Task<ApiResponse<Conversation>> GetOrCreateDirectConversationAsync(Guid targetUserId)
    {
                var response = await _httpClientService.PostAsync<Conversation>($"api/app/chat/get-or-create-direct-conversation/{targetUserId}", new object());
        return response != null
            ? new ApiResponse<Conversation> { Success = true, Data = response }
            : new ApiResponse<Conversation> { Success = false };
    }

    public async Task<ApiResponse<Conversation>> GetOrCreateGroupConversationAsync(Guid groupId)
    {
                var response = await _httpClientService.PostAsync<Conversation>($"api/app/chat/get-or-create-group-conversation/{groupId}", new object());
        return response != null
            ? new ApiResponse<Conversation> { Success = true, Data = response }
            : new ApiResponse<Conversation> { Success = false };
    }

    public async Task<ApiResponse<Conversation>> GetOrCreateDeviceConversationAsync(Guid deviceId)
    {
                var response = await _httpClientService.PostAsync<Conversation>($"api/app/chat/get-or-create-device-conversation/{deviceId}", new object());
        return response != null
            ? new ApiResponse<Conversation> { Success = true, Data = response }
            : new ApiResponse<Conversation> { Success = false };
    }

    public async Task<ApiResponse<List<Conversation>>> GetMyConversationsAsync()
    {
                var response = await _httpClientService.GetAsync<List<Conversation>>("api/app/chat/my-conversations");
        return response != null
            ? new ApiResponse<List<Conversation>> { Success = true, Data = response }
            : new ApiResponse<List<Conversation>> { Success = false };
    }

    public async Task<ApiResponse<List<ChatMessage>>> GetMessagesAsync(Guid conversationId, int skipCount = 0, int maxResultCount = 50)
    {
                var response = await _httpClientService.GetAsync<List<ChatMessage>>($"api/app/chat/messages/{conversationId}?skipCount={skipCount}&maxResultCount={maxResultCount}");
        return response != null
            ? new ApiResponse<List<ChatMessage>> { Success = true, Data = response }
            : new ApiResponse<List<ChatMessage>> { Success = false };
    }

    public async Task<ApiResponse<List<ChatMessage>>> GetUnreadMessagesWithMarkAsync(Guid conversationId)
    {
                var response = await _httpClientService.GetAsync<List<ChatMessage>>($"api/app/chat/unread-messages/{conversationId}");
        return response != null
            ? new ApiResponse<List<ChatMessage>> { Success = true, Data = response }
            : new ApiResponse<List<ChatMessage>> { Success = false };
    }

    public async Task<ApiResponse<ChatMessage>> SendMessageAsync(Guid conversationId, string content, string messageType = "Text")
    {
                var response = await _httpClientService.PostAsync<ChatMessage>(
            $"api/app/chat/send-message/{conversationId}",
            new { Content = content, MessageType = messageType });
        return response != null
            ? new ApiResponse<ChatMessage> { Success = true, Data = response }
            : new ApiResponse<ChatMessage> { Success = false };
    }

    public async Task<ApiResponse<object>> MarkAsReadAsync(Guid conversationId)
    {
                var response = await _httpClientService.PostAsync<object>($"api/app/chat/mark-as-read/{conversationId}", new object());
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }

    public async Task<ApiResponse<object>> DeleteConversationAsync(Guid conversationId)
    {
                var response = await _httpClientService.DeleteAsync<object>($"api/app/chat/delete-conversation/{conversationId}");
        return response != null
            ? new ApiResponse<object> { Success = true, Data = response }
            : new ApiResponse<object> { Success = false };
    }

    public async Task<ApiResponse<int>> GetUnreadCountAsync()
    {
                var response = await _httpClientService.GetAsync<UnreadCountResult>("api/app/chat/unread-count");
        return response != null
            ? new ApiResponse<int> { Success = true, Data = response.Count }
            : new ApiResponse<int> { Success = false, Data = 0 };
    }
}