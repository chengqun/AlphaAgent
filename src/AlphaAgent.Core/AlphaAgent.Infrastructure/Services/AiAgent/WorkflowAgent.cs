using AlphaAgent.Domain.Abstractions.AiAgent;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
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

/// <summary>
/// Adapter that implements IAgent by wrapping an AIAgent produced from a Workflow.
/// Workflow manages its own internal state, so this adapter uses MemoryMode.Stateless.
/// </summary>
public class WorkflowAgent : IAgent
{
    private readonly AIAgent _aiAgent;
    private readonly string _systemPrompt;
    private readonly float? _temperature;

    public string Name { get; }
    public string Description { get; }
    public string? SystemPrompt => _systemPrompt;
    public float? DefaultTemperature => _temperature;
    public IReadOnlyList<ToolInfo> Tools { get; } = Array.Empty<ToolInfo>();
    public AgentMemoryMode MemoryMode { get; } = AgentMemoryMode.Stateless;
    public int MaxHistoryMessages { get; } = 20;

    /// <summary>
    /// 子 Agent 步骤信息（名称、描述、使用的工具列表）。
    /// 在 Workflow 工厂类创建时填充，供 UI 展示工作流结构。
    /// </summary>
    public List<WorkflowSubAgentInfo> SubAgents { get; } = new();

    public WorkflowAgent(
        string name,
        string description,
        string systemPrompt,
        AIAgent aiAgent,
        float? temperature = null)
    {
        Name = name;
        Description = description;
        _systemPrompt = systemPrompt;
        _aiAgent = aiAgent;
        _temperature = temperature;
    }

    public async Task<DomainAgentResponse> RunAsync(
        AgentContext context, CancellationToken cancellationToken = default)
    {
        var chatMessages = ConvertMessages(context);
        var session = await _aiAgent.CreateSessionAsync(cancellationToken);
        var runOptions = new AgentRunOptions();

        var result = await _aiAgent.RunAsync(
            chatMessages, session, runOptions, cancellationToken);

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
        var session = await _aiAgent.CreateSessionAsync(cancellationToken);
        var runOptions = new AgentRunOptions();
        var callIdToName = new Dictionary<string, string>();

        await foreach (var update in _aiAgent.RunStreamingAsync(
            chatMessages, session, runOptions, cancellationToken))
        {
            // Emit text content
            if (!string.IsNullOrEmpty(update.Text))
            {
                yield return new AgentResponseChunk
                {
                    Content = update.Text,
                    IsComplete = false,
                    AuthorName = update.AuthorName
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
                        AuthorName = update.AuthorName,
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
                        AuthorName = update.AuthorName,
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

                    if (msg.ToolCalls != null)
                    {
                        foreach (var tc in msg.ToolCalls)
                        {
                            var inputDict = tc.Input ?? new Dictionary<string, object>();
                            var callId = tc.Id ?? Guid.NewGuid().ToString("N")[..24];
                            assistantContent.Add(new FunctionCallContent(callId, tc.ToolName, inputDict));
                        }
                    }

                    messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, assistantContent));

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

        if (pendingCall != null)
            toolCalls.Add(pendingCall);

        return toolCalls.Count > 0 ? toolCalls : null;
    }

    private static Dictionary<string, object>? ToOutputDictionary(object? result)
    {
        if (result == null) return null;

        if (result is Dictionary<string, object> dict)
            return dict;

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

/// <summary>
/// 工作流子 Agent 信息，供 UI 展示工作流步骤结构。
/// </summary>
public class WorkflowSubAgentInfo
{
    /// <summary>子 Agent 名称（如 "TechnicalAnalyst"）</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>子 Agent 显示名称（如 "技术分析专家"）</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>子 Agent 描述</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>子 Agent 使用的工具列表</summary>
    public List<WorkflowToolInfo> Tools { get; set; } = new();
}

/// <summary>
/// 工作流子 Agent 的工具信息。
/// </summary>
public class WorkflowToolInfo
{
    /// <summary>工具名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>工具描述</summary>
    public string Description { get; set; } = string.Empty;
}