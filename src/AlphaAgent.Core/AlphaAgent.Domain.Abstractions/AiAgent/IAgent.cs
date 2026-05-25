using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Abstractions.AiAgent;

/// <summary>
/// Agent 记忆模式，控制是否加载历史消息和持久化对话。
/// </summary>
public enum AgentMemoryMode
{
    /// <summary>有记忆：加载历史消息，持久化用户和助手消息</summary>
    Stateful,
    /// <summary>无记忆：不加载历史消息，不持久化对话（每次调用都是全新的）</summary>
    Stateless,
    /// <summary>滑动窗口：只保留最近 N 条历史消息</summary>
    SlidingWindow
}

public interface IAgent
{
    string Name { get; }
    string Description { get; }
    string? SystemPrompt { get; }
    float? DefaultTemperature { get; }
    IReadOnlyList<ToolInfo> Tools { get; }

    /// <summary>
    /// 记忆模式，默认 Stateful。AgentService 据此决定是否加载历史和持久化消息。
    /// </summary>
    AgentMemoryMode MemoryMode { get; }

    /// <summary>
    /// 滑动窗口模式下保留的历史消息数量，默认 20。仅在 MemoryMode=SlidingWindow 时生效。
    /// </summary>
    int MaxHistoryMessages { get; }

    Task<AgentResponse> RunAsync(AgentContext context, CancellationToken cancellationToken = default);
    IAsyncEnumerable<AgentResponseChunk> RunStreamingAsync(AgentContext context, CancellationToken cancellationToken = default);
}
