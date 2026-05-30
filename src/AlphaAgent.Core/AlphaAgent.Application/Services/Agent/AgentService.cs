using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentChatMessage = AlphaAgent.Domain.Abstractions.AiAgent.ChatMessage;

namespace AlphaAgent.Application.Services.Agent;

public class AgentService : IAgentService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentFactory _agentFactory;
    private const int DefaultMaxHistoryMessages = 20;

    public AgentService(IAgentRepository agentRepository, IAgentFactory agentFactory)
    {
        _agentRepository = agentRepository;
        _agentFactory = agentFactory;
    }

    public async Task<AgentSessionDto?> GetActiveSessionAsync(Guid userId, string agentName)
    {
        var session = await _agentRepository.GetActiveSessionAsync(userId, agentName);
        if (session == null) return null;

        return new AgentSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            AgentName = session.AgentName,
            CreatedAt = session.CreatedAt,
            LastActiveAt = session.LastActiveAt,
            Status = session.Status.ToString()
        };
    }

    public async Task<AgentSessionDto?> GetActiveSessionByContextAsync(Guid userId, string agentName, string context)
    {
        var session = await _agentRepository.GetActiveSessionByContextAsync(userId, agentName, context);
        if (session == null) return null;

        return new AgentSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            AgentName = session.AgentName,
            CreatedAt = session.CreatedAt,
            LastActiveAt = session.LastActiveAt,
            Status = session.Status.ToString()
        };
    }

    public async Task<AgentSessionDto> StartSessionAsync(Guid userId, string agentName, string? initialContext = null)
    {
        var session = await _agentRepository.CreateSessionAsync(userId, agentName);

        if (!string.IsNullOrWhiteSpace(initialContext))
        {
            session.UpdateContext(initialContext);
            await _agentRepository.UpdateSessionAsync(session);
        }

        return new AgentSessionDto
        {
            Id = session.Id,
            UserId = session.UserId,
            AgentName = session.AgentName,
            CreatedAt = session.CreatedAt,
            LastActiveAt = session.LastActiveAt,
            Status = session.Status.ToString()
        };
    }

    public async Task<AgentResponseDto> SendMessageAsync(Guid sessionId, string message)
    {
        var session = await _agentRepository.GetSessionAsync(sessionId);
        if (session == null)
        {
            throw new InvalidOperationException("会话不存在");
        }

        var agent = _agentFactory.GetAgent(session.AgentName);

        // 持久化用户消息（所有模式都保存，用于聊天记录展示）
        var userMessage = AgentMessage.Create(sessionId, AgentMessageRole.User, message);
        await _agentRepository.AddMessageAsync(userMessage);

        // 根据 MemoryMode 构建聊天历史（Stateless 不传历史给 LLM）
        var chatMessages = BuildChatHistory(session.Messages, agent);
        chatMessages.Add(AgentChatMessage.User(message));

        var context = new AgentContext
        {
            SessionId = sessionId,
            UserId = session.UserId,
            Messages = chatMessages,
            SystemPrompt = agent.SystemPrompt,
            Temperature = agent.DefaultTemperature
        };

        var response = await agent.RunAsync(context);

        var toolCalls = response.ToolCalls?.Select(tc => new ToolCall
        {
            Id = tc.Id,
            ToolName = tc.ToolName,
            Input = tc.Input,
            Output = tc.Output
        }).ToList();

        toolCalls?.ForEach(tc => tc.SerializeJson());

        // 构建 ContentParts：非流式模式下，工具调用在前，文本在后
        var contentParts = new List<ContentPart>();
        var partIndex = 0;
        if (toolCalls != null)
        {
            foreach (var tc in toolCalls)
            {
                contentParts.Add(new ContentPart
                {
                    Type = "tool_call",
                    Index = partIndex++,
                    ToolName = tc.ToolName,
                    ToolInput = tc.Input
                });
                if (tc.Output != null)
                {
                    contentParts.Add(new ContentPart
                    {
                        Type = "tool_result",
                        Index = partIndex++,
                        ToolName = tc.ToolName,
                        ToolOutput = tc.Output
                    });
                }
            }
        }
        if (!string.IsNullOrEmpty(response.Content))
        {
            contentParts.Add(new ContentPart
            {
                Type = "text",
                Index = partIndex++,
                Text = response.Content
            });
        }
        var contentPartsJson = contentParts.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(contentParts)
            : null;

        var assistantMessage = AgentMessage.Create(sessionId, AgentMessageRole.Assistant, response.Content, toolCalls, contentPartsJson);

        // 持久化助手消息（所有模式都保存，用于聊天记录展示）
        await _agentRepository.AddMessageAsync(assistantMessage);

        return new AgentResponseDto
        {
            Content = response.Content,
            IsComplete = response.IsComplete,
            ToolCalls = response.ToolCalls?.Select(tc => new ToolCallDto
            {
                ToolName = tc.ToolName,
                Input = tc.Input,
                Output = tc.Output
            }).ToList()
        };
    }

    public async IAsyncEnumerable<AgentStreamEvent> SendMessageStreamingAsync(Guid sessionId, string message)
    {
        var session = await _agentRepository.GetSessionAsync(sessionId);
        if (session == null)
        {
            yield return new AgentTextEvent("错误：会话不存在");
            yield break;
        }

        var agent = _agentFactory.GetAgent(session.AgentName);

        // 持久化用户消息（所有模式都保存，用于聊天记录展示）
        var userMessage = AgentMessage.Create(sessionId, AgentMessageRole.User, message);
        await _agentRepository.AddMessageAsync(userMessage);

        // 根据 MemoryMode 构建聊天历史（Stateless 不传历史给 LLM）
        var chatMessages = BuildChatHistory(session.Messages, agent);
        chatMessages.Add(AgentChatMessage.User(message));

        var context = new AgentContext
        {
            SessionId = sessionId,
            UserId = session.UserId,
            Messages = chatMessages,
            SystemPrompt = agent.SystemPrompt,
            Temperature = agent.DefaultTemperature
        };

        var contentBuilder = new System.Text.StringBuilder();
        var toolCallMap = new Dictionary<string, ToolCall>();
        var contentParts = new List<ContentPart>();
        var partIndex = 0;
        var currentTextPartStart = contentBuilder.Length;
        string? lastAuthorName = null;

        await foreach (var chunk in agent.RunStreamingAsync(context))
        {
            if (chunk.AuthorName != null)
                lastAuthorName = chunk.AuthorName;

            if (!string.IsNullOrEmpty(chunk.Content))
            {
                contentBuilder.Append(chunk.Content);
                yield return new AgentTextEvent(chunk.Content) { AuthorName = chunk.AuthorName };
            }

            if (chunk.ToolCall != null)
            {
                // 文本和工具调用交错：遇到工具调用时，先记录之前的文本段
                if (contentBuilder.Length > currentTextPartStart)
                {
                    var textSoFar = contentBuilder.ToString(currentTextPartStart, contentBuilder.Length - currentTextPartStart);
                    contentParts.Add(new ContentPart
                    {
                        Type = "text",
                        Index = partIndex++,
                        Text = textSoFar,
                        AuthorName = chunk.AuthorName
                    });
                    currentTextPartStart = contentBuilder.Length;
                }

                var callId = chunk.ToolCall.Id;
                if (chunk.ToolCall.Output == null)
                {
                    toolCallMap[callId] = chunk.ToolCall;

                    contentParts.Add(new ContentPart
                    {
                        Type = "tool_call",
                        Index = partIndex++,
                        ToolName = chunk.ToolCall.ToolName,
                        ToolInput = chunk.ToolCall.Input,
                        AuthorName = chunk.AuthorName
                    });

                    yield return new AgentToolCallEvent
                    {
                        ToolName = chunk.ToolCall.ToolName,
                        Input = chunk.ToolCall.Input,
                        AuthorName = chunk.AuthorName
                    };
                }
                else if (toolCallMap.TryGetValue(callId, out var existing))
                {
                    existing.Output = chunk.ToolCall.Output;

                    contentParts.Add(new ContentPart
                    {
                        Type = "tool_result",
                        Index = partIndex++,
                        ToolName = existing.ToolName,
                        ToolOutput = chunk.ToolCall.Output,
                        AuthorName = chunk.AuthorName
                    });

                    yield return new AgentToolResultEvent
                    {
                        ToolName = existing.ToolName,
                        Output = chunk.ToolCall.Output,
                        AuthorName = chunk.AuthorName
                    };
                }
                else
                {
                    toolCallMap[callId] = chunk.ToolCall;

                    contentParts.Add(new ContentPart
                    {
                        Type = "tool_result",
                        Index = partIndex++,
                        ToolName = chunk.ToolCall.ToolName,
                        ToolOutput = chunk.ToolCall.Output,
                        AuthorName = chunk.AuthorName
                    });

                    yield return new AgentToolResultEvent
                    {
                        ToolName = chunk.ToolCall.ToolName,
                        Output = chunk.ToolCall.Output,
                        AuthorName = chunk.AuthorName
                    };
                }
            }
        }

        // 记录最后的文本段
        if (contentBuilder.Length > currentTextPartStart)
        {
            var remainingText = contentBuilder.ToString(currentTextPartStart, contentBuilder.Length - currentTextPartStart);
            contentParts.Add(new ContentPart
            {
                Type = "text",
                Index = partIndex++,
                Text = remainingText,
                AuthorName = lastAuthorName
            });
        }

        var toolCalls = toolCallMap.Values.ToList();
        toolCalls.ForEach(tc => tc.SerializeJson());
        var contentPartsJson = contentParts.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(contentParts)
            : null;
        var assistantMessage = AgentMessage.Create(
            sessionId, AgentMessageRole.Assistant, contentBuilder.ToString(),
            toolCalls.Count > 0 ? toolCalls : null, contentPartsJson);

        // 持久化助手消息（所有模式都保存，用于聊天记录展示）
        await _agentRepository.AddMessageAsync(assistantMessage);
    }

    public async Task<List<AgentChatMessageDto>> GetSessionHistoryAsync(Guid sessionId, int? limit = null)
    {
        var messages = await _agentRepository.GetSessionMessagesAsync(sessionId, limit);

        return messages.Select(m =>
        {
            List<ContentPart>? contentParts = null;
            if (!string.IsNullOrEmpty(m.ContentPartsJson))
            {
                try
                {
                    contentParts = System.Text.Json.JsonSerializer.Deserialize<List<ContentPart>>(m.ContentPartsJson);
                }
                catch
                {
                    contentParts = null;
                }
            }

            return new AgentChatMessageDto
            {
                Id = m.Id,
                Role = m.Role.ToString(),
                Content = m.Content,
                Timestamp = m.Timestamp,
                ToolCalls = m.ToolCalls?.Select(tc =>
                {
                    tc.DeserializeJson();
                    return new ToolCallDto
                    {
                        ToolName = tc.ToolName,
                        Input = tc.Input,
                        Output = tc.Output
                    };
                }).ToList(),
                ToolCallId = m.ToolCallId,
                ContentParts = contentParts
            };
        }).ToList();
    }

    public Task<List<AgentInfoDto>> GetAvailableAgentsAsync()
    {
        var agents = _agentFactory.GetAvailableAgents();
        return Task.FromResult(agents.Select(a => new AgentInfoDto
        {
            Name = a.Name,
            Description = a.Description,
            SystemPrompt = a.SystemPrompt,
            Tools = a.Tools.Select(t => new ToolInfoDto
            {
                Name = t.Name,
                Description = t.Description
            }).ToList()
        }).ToList());
    }

    public async Task CloseSessionAsync(Guid sessionId)
    {
        var session = await _agentRepository.GetSessionAsync(sessionId);
        if (session != null)
        {
            session.Close();
            await _agentRepository.UpdateSessionAsync(session);
        }
    }

    private static List<AgentChatMessage> BuildChatHistory(List<AgentMessage> persistedMessages, IAgent agent)
    {
        // Stateless 模式：不加载任何历史
        if (agent.MemoryMode == AgentMemoryMode.Stateless)
            return new List<AgentChatMessage>();

        var maxMessages = agent.MemoryMode == AgentMemoryMode.SlidingWindow
            ? agent.MaxHistoryMessages
            : int.MaxValue;

        var recentMessages = persistedMessages
            .OrderBy(m => m.Timestamp)
            .TakeLast(maxMessages)
            .ToList();

        return recentMessages.Select(m => new AgentChatMessage
        {
            Role = m.Role switch
            {
                AgentMessageRole.User => "user",
                AgentMessageRole.Assistant => "assistant",
                AgentMessageRole.System => "system",
                AgentMessageRole.Tool => "tool",
                _ => "user"
            },
            Content = m.Content,
            ToolCalls = m.ToolCalls?.Select(tc =>
            {
                tc.DeserializeJson();
                return new ToolCall
                {
                    Id = tc.Id,
                    ToolName = tc.ToolName,
                    Input = tc.Input,
                    Output = tc.Output
                };
            }).ToList(),
            ToolCallId = m.ToolCallId
        }).ToList();
    }
}
