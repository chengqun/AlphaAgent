using System;

namespace AlphaAgent.Maui.Events;

public class UnreadCountUpdatedEvent
{
    public Guid ConversationId { get; }
    public Guid UserId { get; }
    public int UnreadCount { get; }

    public UnreadCountUpdatedEvent(Guid conversationId, Guid userId, int unreadCount)
    {
        ConversationId = conversationId;
        UserId = userId;
        UnreadCount = unreadCount;
    }
}