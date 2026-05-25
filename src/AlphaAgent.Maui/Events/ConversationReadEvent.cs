using System;

namespace AlphaAgent.Maui.Events;

public class ConversationReadEvent
{
    public Guid ConversationId { get; set; }
    
    public ConversationReadEvent(Guid conversationId)
    {
        ConversationId = conversationId;
    }
}