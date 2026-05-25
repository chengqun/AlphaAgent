using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AlphaAgent.Abp.Domain.Entities;

public class AppAgentConfig : FullAuditedAggregateRoot<Guid>
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public float Temperature { get; set; } = 0.5f;
    public bool IsActive { get; set; } = true;

    public AppAgentConfig() { }

    public AppAgentConfig(
        string agentName,
        string modelName,
        string apiKey,
        string endpoint,
        string defaultSystemPrompt,
        float temperature = 0.5f,
        bool isActive = true)
    {
        AgentName = agentName;
        ModelName = modelName;
        ApiKey = apiKey;
        Endpoint = endpoint;
        DefaultSystemPrompt = defaultSystemPrompt;
        Temperature = temperature;
        IsActive = isActive;
    }
}