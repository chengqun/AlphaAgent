using AlphaAgent.Domain.Abstractions.AiAgent;
using System;
using System.Collections.Generic;

namespace AlphaAgent.Domain.Entities;

public class AgentSession
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string AgentName { get; private set; } = string.Empty;
    public string? Context { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastActiveAt { get; private set; }
    public AgentSessionStatus Status { get; set; }

    public List<AgentMessage> Messages { get; private set; } = new();

    protected AgentSession() { }

    public AgentSession(Guid userId, string agentName)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        AgentName = agentName;
        CreatedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
        Status = AgentSessionStatus.Active;
    }

    public void AddMessage(AgentMessageRole role, string content, List<ToolCall>? toolCalls = null)
    {
        Messages.Add(AgentMessage.Create(Id, role, content, toolCalls));
        LastActiveAt = DateTime.UtcNow;
    }

    public void Close() => Status = AgentSessionStatus.Closed;

    public void UpdateContext(string context) => Context = context;
}