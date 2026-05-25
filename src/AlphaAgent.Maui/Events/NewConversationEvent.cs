using AlphaAgent.Application.Dtos.Chat;

namespace AlphaAgent.Maui.Events;

public class NewConversationEvent
{
    public Conversation Conversation { get; }

    public NewConversationEvent(Conversation conversation)
    {
        Conversation = conversation;
    }
}
