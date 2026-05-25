using System;
using System.Collections.Generic;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

public class AgentContext
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public List<ChatMessage> Messages { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? SystemPrompt { get; set; }
    public float? Temperature { get; set; }
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<ToolCall>? ToolCalls { get; set; }
    public string? ToolCallId { get; set; }

    public static ChatMessage User(string content) => new() { Role = "user", Content = content };
    public static ChatMessage Assistant(string content) => new() { Role = "assistant", Content = content };
    public static ChatMessage System(string content) => new() { Role = "system", Content = content };
    public static ChatMessage Tool(string content, string toolCallId) => new() { Role = "tool", Content = content, ToolCallId = toolCallId };
}

public class ToolCall
{
    public string Id { get; set; } = string.Empty;
    public string ToolName { get; set; } = string.Empty;
    public Dictionary<string, object> Input { get; set; } = new();
    public Dictionary<string, object>? Output { get; set; }

    public string? InputJson { get; set; }
    public string? OutputJson { get; set; }

    public void SerializeJson()
    {
        InputJson = Input.Count > 0 ? System.Text.Json.JsonSerializer.Serialize(Input) : null;
        OutputJson = Output != null ? System.Text.Json.JsonSerializer.Serialize(Output) : null;
    }

    public void DeserializeJson()
    {
        if (InputJson != null)
        {
            try { Input = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(InputJson) ?? new(); }
            catch { Input = new(); }
        }
        if (OutputJson != null)
        {
            try { Output = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(OutputJson); }
            catch { Output = null; }
        }
    }
}

public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = true;
    public List<ToolCall>? ToolCalls { get; set; }
}

public class AgentResponseChunk
{
    public string Content { get; set; } = string.Empty;
    public bool IsComplete { get; set; } = false;
    public ToolCall? ToolCall { get; set; }
}

public enum AgentSessionStatus
{
    Active,
    Closed,
    Expired
}