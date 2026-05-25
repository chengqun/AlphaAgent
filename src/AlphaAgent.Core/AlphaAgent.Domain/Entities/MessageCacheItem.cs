using System;

namespace AlphaAgent.Domain.Entities;

public class MessageCacheItem
{
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public string MessageData { get; private set; } = string.Empty;
    public DateTime LastCachedAt { get; private set; }
    public DateTime LastMessageTime { get; private set; }

    private MessageCacheItem() { }

    public MessageCacheItem(Guid conversationId, string messageData)
    {
        Id = Guid.NewGuid();
        ConversationId = conversationId;
        MessageData = messageData;
        LastCachedAt = DateTime.UtcNow;
        LastMessageTime = DateTime.UtcNow;
    }

    public void MarkUpdated(string messageData)
    {
        MessageData = messageData;
        LastCachedAt = DateTime.UtcNow;
    }
}