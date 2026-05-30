using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AlphaAgent.Abp.Domain.Entities;

public class AppAgentConfig : FullAuditedAggregateRoot<Guid>
{
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// 关联的 LLM 配置 ID。null 表示使用默认 LLM 配置（AppLlmConfig.IsDefault=true 的那条）。
    /// </summary>
    public Guid? LlmConfigId { get; set; }

    public AppAgentConfig() { }

    public AppAgentConfig(
        string agentName,
        string defaultSystemPrompt,
        bool isActive = true,
        Guid? llmConfigId = null)
    {
        AgentName = agentName;
        DefaultSystemPrompt = defaultSystemPrompt;
        IsActive = isActive;
        LlmConfigId = llmConfigId;
    }
}
