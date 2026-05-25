using AlphaAgent.Domain.Abstractions.Chat;

namespace AlphaAgent.Maui.Events;

public class NewMessageEvent
{
    public ChatMessage Message { get; }

    public NewMessageEvent(ChatMessage message)
    {
        Message = message;
    }
}