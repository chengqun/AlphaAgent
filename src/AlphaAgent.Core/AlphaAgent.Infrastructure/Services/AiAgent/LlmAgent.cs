using AlphaAgent.Domain.Abstractions.AiAgent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DomainAgentResponse = AlphaAgent.Domain.Abstractions.AiAgent.AgentResponse;

namespace AlphaAgent.Infrastructure.Services.AiAgent;

public class LlmAgent : IAgent
{
    private readonly ChatClientAgent _chatClientAgent;
    private readonly string _systemPrompt;
    private readonly float? _temperature;
    private readonly IReadOnlyList<AITool> _tools;

    public string Name { get; }
    public string Description { get; }
    public string? SystemPrompt => _systemPrompt;
    public float? DefaultTemperature => _temperature;
    public IReadOnlyList<ToolInfo> Tools { get; }
    public AgentMemoryMode MemoryMode { get; }
    public int MaxHistoryMessages { get; }

    public LlmAgent(
        string name,
        string description,
        string systemPrompt,
        ChatClientAgent chatClientAgent,
        IReadOnlyList<AITool> tools,
        float? temperature = null,
        AgentMemoryMode memoryMode = AgentMemoryMode.SlidingWindow,
        int maxHistoryMessages = 20)
    {
        Name = name;
        Description = description;
        _systemPrompt = systemPrompt;
        _chatClientAgent = chatClientAgent;
        _tools = tools;
        _temperature = temperature;
        MemoryMode = memoryMode;
        MaxHistoryMessages = maxHistoryMessages;
        Tools = tools.Select(t => new ToolInfo { Name = t.Name, Description = t.Description ?? string.Empty }).ToList().AsReadOnly();
    }

    public async Task<DomainAgentResponse> RunAsync(
        AgentContext context, CancellationToken cancellationToken = default)
    {
        var chatMessages = ConvertMessages(context);
        var runOptions = CreateRunOptions();

        var result = await _chatClientAgent.RunAsync(
            chatMessages, null, runOptions, cancellationToken);

        return new DomainAgentResponse
        {
            Content = result.Text ?? string.Empty,
            IsComplete = true,
            ToolCalls = ExtractToolCalls(result)
        };
    }

    public async IAsyncEnumerable<AgentResponseChunk> RunStreamingAsync(
        AgentContext context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatMessages = ConvertMessages(context);
        var runOptions = CreateRunOptions();
        var callIdToName = new Dictionary<string, string>();

        await foreach (var update in _chatClientAgent.RunStreamingAsync(
            chatMessages, null, runOptions, cancellationToken))
        {
            // Emit text content
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new AgentResponseChunk
                {
                    Content = update.Text,
                    IsComplete = false
                };
            }

            // Emit tool call events from contents
            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    if (!string.IsNullOrEmpty(functionCall.CallId) && !string.IsNullOrEmpty(functionCall.Name))
                        callIdToName[functionCall.CallId] = functionCall.Name;

                    var input = functionCall.Arguments?
                        .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                        .Where(kvp => kvp.Value != null)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!)
                        ?? new();

                    yield return new AgentResponseChunk
                    {
                        Content = string.Empty,
                        IsComplete = false,
                        ToolCall = new ToolCall
                        {
                            Id = functionCall.CallId,
                            ToolName = functionCall.Name,
                            Input = input
                        }
                    };
                }
                else if (content is FunctionResultContent functionResult)
                {
                    var toolName = functionResult.CallId != null && callIdToName.TryGetValue(functionResult.CallId, out var name)
                        ? name : string.Empty;

                    yield return new AgentResponseChunk
                    {
                        Content = string.Empty,
                        IsComplete = false,
                        ToolCall = new ToolCall
                        {
                            Id = functionResult.CallId,
                            ToolName = toolName,
                            Output = ToOutputDictionary(functionResult.Result)
                        }
                    };
                }
            }
        }

        yield return new AgentResponseChunk { Content = string.Empty, IsComplete = true };
    }

    private List<Microsoft.Extensions.AI.ChatMessage> ConvertMessages(AgentContext context)
    {
        var messages = new List<Microsoft.Extensions.AI.ChatMessage>();

        var systemPrompt = context.SystemPrompt ?? _systemPrompt;
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, systemPrompt));
        }

        foreach (var msg in context.Messages)
        {
            switch (msg.Role)
            {
                case "user":
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, msg.Content));
                    break;
                case "assistant":
                    var assistantContent = new List<AIContent>();
                    if (!string.IsNullOrEmpty(msg.Content))
                        assistantContent.Add(new TextContent(msg.Content));

                    // 将历史工具调用转换为 FunctionCallContent
                    if (msg.ToolCalls != null)
                    {
                        foreach (var tc in msg.ToolCalls)
                        {
                            var inputDict = tc.Input ?? new Dictionary<string, object>();
                            var callId = tc.Id ?? Guid.NewGuid().ToString("N")[..24];
                            assistantContent.Add(new FunctionCallContent(callId, tc.ToolName, inputDict));
                        }
                    }

                    // assistant 消息必须先添加，tool result 紧随其后
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, assistantContent));

                    // 工具结果紧跟在 assistant 消息后面
                    if (msg.ToolCalls != null)
                    {
                        foreach (var tc in msg.ToolCalls)
                        {
                            if (tc.Output != null)
                            {
                                var callId = tc.Id ?? Guid.NewGuid().ToString("N")[..24];
                                var resultContent = new List<AIContent>
                                {
                                    new FunctionResultContent(callId, tc.Output)
                                };
                                messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, resultContent));
                            }
                        }
                    }
                    break;
                case "system":
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, msg.Content));
                    break;
                case "tool":
                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, msg.Content));
                    break;
            }
        }

        return messages;
    }

    private ChatClientAgentRunOptions CreateRunOptions()
    {
        var chatOptions = new ChatOptions
        {
            Tools = _tools.ToList(),
            Temperature = _temperature ?? 0.5f
        };

        return new ChatClientAgentRunOptions(chatOptions);
    }

    private static List<ToolCall>? ExtractToolCalls(Microsoft.Agents.AI.AgentResponse result)
    {
        var toolCalls = new List<ToolCall>();
        ToolCall? pendingCall = null;

        foreach (var message in result.Messages)
        {
            foreach (var content in message.Contents)
            {
                if (content is FunctionCallContent functionCall)
                {
                    var input = functionCall.Arguments?
                        .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                        .Where(kvp => kvp.Value != null)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value!)
                        ?? new();

                    pendingCall = new ToolCall
                    {
                        Id = functionCall.CallId,
                        ToolName = functionCall.Name,
                        Input = input
                    };
                }
                else if (content is FunctionResultContent functionResult && pendingCall != null)
                {
                    pendingCall.Output = ToOutputDictionary(functionResult.Result);
                    toolCalls.Add(pendingCall);
                    pendingCall = null;
                }
            }
        }

        // 如果有 FunctionCall 但没有对应的 FunctionResult，也保留
        if (pendingCall != null)
            toolCalls.Add(pendingCall);

        return toolCalls.Count > 0 ? toolCalls : null;
    }

    private static Dictionary<string, object>? ToOutputDictionary(object? result)
    {
        if (result == null) return null;

        // 如果结果本身就是 Dictionary，直接返回
        if (result is Dictionary<string, object> dict)
            return dict;

        // 如果是 string，尝试 JSON 反序列化为 Dictionary
        if (result is string json)
        {
            try
            {
                var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (deserialized != null) return deserialized;
            }
            catch { }

            return new() { ["Result"] = json };
        }

        // 其他类型，序列化为 JSON 再反序列化为 Dictionary
        try
        {
            var serialized = JsonSerializer.Serialize(result);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(serialized);
        }
        catch
        {
            return new() { ["Result"] = result.ToString() ?? string.Empty };
        }
    }
}
