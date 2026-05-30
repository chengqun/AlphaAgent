using System;
using System.Collections.Generic;

namespace AlphaAgent.Domain.Entities;

public class AgentConfigCacheItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string AgentName { get; set; } = "指标分析Agent";
    public string DefaultSystemPrompt { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    /// <summary>关联的 LLM 配置 ID。null 表示使用默认 LLM 配置。</summary>
    public Guid? LlmConfigId { get; set; }

    /// <summary>每个 Agent 启用的 tool 名称列表（内存使用）</summary>
    public List<string> EnabledTools { get; set; } = new();

    /// <summary>SQLite 存储 JSON backing field</summary>
    public string? EnabledToolsJson { get; set; }

    public DateTime CachedAt { get; set; }

    public void SerializeEnabledTools()
    {
        EnabledToolsJson = EnabledTools.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(EnabledTools)
            : null;
    }

    public void DeserializeEnabledTools()
    {
        if (EnabledToolsJson != null)
        {
            try { EnabledTools = System.Text.Json.JsonSerializer.Deserialize<List<string>>(EnabledToolsJson) ?? new(); }
            catch { EnabledTools = new(); }
        }
        else
        {
            EnabledTools = new();
        }
    }
}