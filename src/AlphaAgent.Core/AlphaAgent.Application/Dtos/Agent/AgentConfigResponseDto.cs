using System;
using System.Collections.Generic;

namespace AlphaAgent.Application.Dtos.Agent;

public class AgentConfigResponseDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? LlmConfigId { get; set; }
    public List<string> EnabledTools { get; set; } = new();
}
