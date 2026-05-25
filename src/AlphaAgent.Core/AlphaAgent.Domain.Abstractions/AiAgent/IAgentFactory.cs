using System;
using System.Collections.Generic;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

public interface IAgentFactory
{
    IAgent GetAgent(string agentName);
    IReadOnlyList<AgentInfo> GetAvailableAgents();
}

public class AgentInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public IReadOnlyList<ToolInfo> Tools { get; set; } = Array.Empty<ToolInfo>();
    public AgentMemoryMode MemoryMode { get; set; } = AgentMemoryMode.Stateful;
    public int MaxHistoryMessages { get; set; } = 20;
}

public class ToolInfo
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
