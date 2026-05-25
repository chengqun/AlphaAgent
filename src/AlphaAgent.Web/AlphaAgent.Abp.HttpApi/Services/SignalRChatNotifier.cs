using System;
using System.Threading.Tasks;
using AlphaAgent.Abp.Application.Contracts.DTOs.Chat;
using AlphaAgent.Abp.Application.Contracts.Services.Chat;
using AlphaAgent.Abp.HttpApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AlphaAgent.Abp.HttpApi.Services;

public class SignalRChatNotifier : IChatNotifier
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRChatNotifier(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyMessageAsync(Guid conversationId, ChatMessageDto message, Guid senderId)
    {
        // 广播给会话组中所有连接（发送者的回声由客户端根据 SenderId 过滤）
        await _hubContext.Clients.Group($"conv_{conversationId}")
            .SendAsync("ReceiveMessage", message);
    }

    public async Task NotifyUnreadCountUpdatedAsync(Guid conversationId, Guid userId, int unreadCount)
    {
        await _hubContext.Clients.Group($"conv_{conversationId}")
            .SendAsync("UnreadCountUpdated", conversationId, userId, unreadCount);
    }
}
