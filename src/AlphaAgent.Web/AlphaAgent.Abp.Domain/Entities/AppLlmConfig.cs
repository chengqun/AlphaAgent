using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace AlphaAgent.Abp.Domain.Entities;

/// <summary>
/// LLM 配置：每个用户可有多条（如 DeepSeek、GPT-4o、本地模型）。
/// 各 Agent 通过 AppAgentConfig.LlmConfigId 选择使用哪条 LLM 配置。
/// </summary>
public class AppLlmConfig : FullAuditedAggregateRoot<Guid>
{
    /// <summary>配置名称，如"DeepSeek"、"GPT-4o"</summary>
    public string Name { get; set; } = string.Empty;

    public string ModelName { get; set; } = "deepseek-chat";

    public string ApiKey { get; set; } = string.Empty;

    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";

    public float Temperature { get; set; } = 0.5f;

    /// <summary>是否为默认配置（每个用户至少一条为 true）</summary>
    public bool IsDefault { get; set; } = false;

    public AppLlmConfig() { }

    public AppLlmConfig(
        string name,
        string modelName,
        string apiKey,
        string endpoint,
        float temperature = 0.5f,
        bool isDefault = false)
    {
        Name = name;
        ModelName = modelName;
        ApiKey = apiKey;
        Endpoint = endpoint;
        Temperature = temperature;
        IsDefault = isDefault;
    }
}
