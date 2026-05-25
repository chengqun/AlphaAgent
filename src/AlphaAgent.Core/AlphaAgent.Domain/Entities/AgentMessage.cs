using AlphaAgent.Domain.Abstractions.AiAgent;
using System;
using System.Collections.Generic;

namespace AlphaAgent.Domain.Entities;

public enum AgentMessageRole
{
    User,
    Assistant,
    System,
    Tool
}

public class AgentMessage
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public AgentMessageRole Role { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public List<ToolCall>? ToolCalls { get; private set; }
    public string? ToolCallId { get; private set; }
    /// <summary>
    /// 记录消息中各内容片段的交错顺序（JSON 序列化），用于加载历史时按正确顺序展示。
    /// </summary>
    public string? ContentPartsJson { get; private set; }

    private AgentMessage() { }

    public static AgentMessage Create(Guid sessionId, AgentMessageRole role, string content, List<ToolCall>? toolCalls = null, string? contentPartsJson = null)
    {
        return new AgentMessage
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            ToolCalls = toolCalls,
            ContentPartsJson = contentPartsJson
        };
    }
}