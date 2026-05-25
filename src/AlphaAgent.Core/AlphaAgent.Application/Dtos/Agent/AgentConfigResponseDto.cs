using System;

namespace AlphaAgent.Application.Dtos.Agent;

public class AgentConfigResponseDto
{
    public Guid Id { get; set; }
    public string AgentName { get; set; } = "指标分析Agent";
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.5f;
    public bool IsActive { get; set; } = true;
}