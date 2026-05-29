using System.Collections.Generic;
using System.Linq;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

public class AgentOptions
{
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public int? MaxTokens { get; set; }

    public Dictionary<string, string> AgentSystemPrompts { get; set; } = new();

    /// <summary>
    /// 每个 Agent 启用的 tool 名称列表。null = 加载全部，空列表 = 不加载任何 tool。
    /// </summary>
    public Dictionary<string, List<string>> EnabledTools { get; set; } = new();

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