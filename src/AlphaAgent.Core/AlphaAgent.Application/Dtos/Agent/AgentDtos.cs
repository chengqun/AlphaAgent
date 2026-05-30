using System;
using System.Collections.Generic;

namespace AlphaAgent.Application.Dtos.Agent;

public class AgentSessionDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class AgentResponseDto
{
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public List<ToolCallDto>? ToolCalls { get; set; }
}

public class ToolCallDto
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Input { get; set; } = new();
    public Dictionary<string, object>? Output { get; set; }
}

/// <summary>
/// 记录 assistant 消息中各内容片段的交错顺序，
/// 使加载历史时能按实际流式顺序（文本→工具调用→工具结果→文本…）展示。
/// </summary>
public class ContentPart
{
    public string Type { get; set; } = string.Empty; // "text" | "tool_call" | "tool_result"
    public int Index { get; set; }
    public string? Text { get; set; }
    public string? ToolName { get; set; }
    public Dictionary<string, object>? ToolInput { get; set; }
    public Dictionary<string, object>? ToolOutput { get; set; }
    /// <summary>
    /// 产出此内容片段的 Agent 名称（多 Agent 工作流中使用）。
    /// </summary>
    public string? AuthorName { get; set; }
}

/// <summary>
/// 流式事件基类，区分文本内容和工具调用
/// </summary>
public abstract class AgentStreamEvent
{
    public string Type { get; protected set; } = string.Empty;
    /// <summary>
    /// 产出此事件的 Agent 名称（多 Agent 工作流中标识哪个子 Agent 在执行）。
    /// 单 Agent 场景下为空，UI 应回退到会话级 AgentName。
    /// </summary>
    public string? AuthorName { get; set; }
}

public class AgentTextEvent : AgentStreamEvent
{
    public string Content { get; set; } = string.Empty;

    public AgentTextEvent() => Type = "text";
    public AgentTextEvent(string content) : this() => Content = content;
}

public class AgentToolCallEvent : AgentStreamEvent
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Input { get; set; } = new();

    public AgentToolCallEvent() => Type = "tool_call";
}

public class AgentToolResultEvent : AgentStreamEvent
{
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object>? Output { get; set; }

    public AgentToolResultEvent() => Type = "tool_result";
}

public class AgentChatMessageDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public List<ToolCallDto>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }
    /// <summary>
    /// 按流式输出顺序排列的内容片段，加载历史时优先使用此字段恢复交错展示。
    /// </summary>
    public List<ContentPart>? ContentParts { get; set; }
}

public class AgentInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public List<ToolInfoDto> Tools { get; set; } = new();
}

public class ToolInfoDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}