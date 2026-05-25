using System.Collections.Generic;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

public class AgentOptions
{
    public string ModelName { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public string Endpoint { get; set; } = "https://api.deepseek.com/v1";
    public float Temperature { get; set; } = 0.5f;
    public int? MaxTokens { get; set; }

    public Dictionary<string, string> AgentSystemPrompts { get; set; } = new();

    public string GetSystemPrompt(string agentName, string fallbackPrompt)
    {
        return AgentSystemPrompts.TryGetValue(agentName, out var prompt) && !string.IsNullOrWhiteSpace(prompt)
            ? prompt : fallbackPrompt;
    }
}