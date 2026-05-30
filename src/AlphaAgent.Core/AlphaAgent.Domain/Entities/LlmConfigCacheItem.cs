using System;

namespace AlphaAgent.Domain.Entities;

/// <summary>
/// LLM 配置缓存项：每个用户可有多条（如 DeepSeek、GPT-4o、本地模型）。
/// </summary>
public class LlmConfigCacheItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public bool IsDefault { get; set; } = false;
    public DateTime CachedAt { get; set; }
}
