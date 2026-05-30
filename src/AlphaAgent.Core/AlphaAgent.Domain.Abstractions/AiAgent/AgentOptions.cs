using System.Collections.Generic;
using System.Linq;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

/// <summary>
/// LLM 连接配置：每条记录代表一个 LLM 后端（如 DeepSeek、GPT-4o、本地模型）。
/// </summary>
public class LlmOptions
{
    public string Name { get; set; } = string.Empty;
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public int? MaxTokens { get; set; }
}

/// <summary>
/// Agent 运行时配置：默认 LLM + 各 Agent 的 LLM 覆盖 + 提示词 + 工具开关。
/// </summary>
public class AgentOptions
{
    /// <summary>默认 LLM 配置（IsDefault=true 的那条）</summary>
    public LlmOptions DefaultLlm { get; set; } = new();

    /// <summary>每个 Agent 指定的 LLM 配置（key = agentName）</summary>
    public Dictionary<string, LlmOptions> AgentLlmConfigs { get; set; } = new();

    public Dictionary<string, string> AgentSystemPrompts { get; set; } = new();

    /// <summary>
    /// 每个 Agent 启用的 tool 名称列表。null = 加载全部，空列表 = 不加载任何 tool。
    /// </summary>
    public Dictionary<string, List<string>> EnabledTools { get; set; } = new();

    /// <summary>
    /// 获取指定 Agent 的 LLM 配置：有特定配置则用特定，否则用默认。
    /// </summary>
    public LlmOptions GetLlmConfig(string agentName) =>
        AgentLlmConfigs.TryGetValue(agentName, out var llm) ? llm : DefaultLlm;

    public string GetSystemPrompt(string agentName, string fallbackPrompt)
    {
        return AgentSystemPrompts.TryGetValue(agentName, out var prompt) && !string.IsNullOrWhiteSpace(prompt)
            ? prompt : fallbackPrompt;
    }

    public List<string>? GetEnabledTools(string agentName)
    {
        return EnabledTools.TryGetValue(agentName, out var tools) ? tools : null;
    }
}
