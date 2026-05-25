using AlphaAgent.Maui.Models;

namespace AlphaAgent.Maui.Views;

public class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? UserTextTemplate { get; set; }
    public DataTemplate? AssistantTextTemplate { get; set; }
    public DataTemplate? ToolCallTemplate { get; set; }
    public DataTemplate? ToolResultTemplate { get; set; }
    public DataTemplate? ThinkingTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
    {
        if (item is ChatMessageItem message)
        {
            switch (message.ItemType)
            {
                case "text" when message.Role == "user":
                    return UserTextTemplate!;
                case "text" when message.Role == "assistant":
                    return AssistantTextTemplate!;
                case "tool_call":
                    return ToolCallTemplate!;
                case "tool_result":
                    return ToolResultTemplate!;
                case "thinking":
                    return ThinkingTemplate!;
            }
        }

        return AssistantTextTemplate!;
    }
}