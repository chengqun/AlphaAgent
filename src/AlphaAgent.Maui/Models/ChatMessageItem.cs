using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AlphaAgent.Maui.Models;

public partial class ChatMessageItem : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _itemType = "text";

    [ObservableProperty]
    private string _role = "";

    [ObservableProperty]
    private string _content = string.Empty;

    /// <summary>
    /// Markdown 渲染内容，流式结束后一次性赋值。
    /// </summary>
    [ObservableProperty]
    private string _markdownContent = string.Empty;

    [ObservableProperty]
    private string _toolName = string.Empty;

    [ObservableProperty]
    private Dictionary<string, object>? _input;

    [ObservableProperty]
    private Dictionary<string, object>? _output;

    [ObservableProperty]
    private DateTime _timestamp;

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// 流式输出中：true → 文本末尾带光标。
    /// </summary>
    [ObservableProperty]
    private bool _isStreaming;

    public bool IsUserText => Role == "user" && ItemType == "text";
    public bool IsAssistantText => Role == "assistant" && ItemType == "text";
    public bool IsToolCall => ItemType == "tool_call";
    public bool IsToolResult => ItemType == "tool_result";
    public bool IsThinking => ItemType == "thinking";
}
