using AlphaAgent.Domain.Abstractions.Chat;
using System;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Abstractions.Interfaces;

public interface ISignalRChatService
{
    bool IsConnected { get; }
    event Func<ChatMessage, Task>? OnMessageReceived;
    event Func<Guid, Guid, int, Task>? OnUnreadCountUpdated;
    event Func<Task>? OnReconnected;

    Task ConnectAsync(string accessToken, string baseUrl);
    Task DisconnectAsync();
    Task JoinConversationAsync(Guid conversationId);
}
