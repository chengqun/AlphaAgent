using System;

namespace AlphaAgent.Domain.Abstractions.Chat;

public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid SenderId { get; set; }
    public string SenderName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string MessageType { get; set; } = "Text";
    public DateTime SentAt { get; set; }
    public bool IsMine { get; set; }
}
