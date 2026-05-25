using AlphaAgent.Application.Dtos.Agent;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Agent;

public interface IAgentService
{
    Task<AgentSessionDto?> GetActiveSessionAsync(Guid userId, string agentName);
    Task<AgentSessionDto?> GetActiveSessionByContextAsync(Guid userId, string agentName, string context);
    Task<AgentSessionDto> StartSessionAsync(Guid userId, string agentName, string? initialContext = null);
    Task<AgentResponseDto> SendMessageAsync(Guid sessionId, string message);
    IAsyncEnumerable<AgentStreamEvent> SendMessageStreamingAsync(Guid sessionId, string message);
    Task<List<AgentChatMessageDto>> GetSessionHistoryAsync(Guid sessionId, int? limit = null);
    Task<List<AgentInfoDto>> GetAvailableAgentsAsync();
    Task CloseSessionAsync(Guid sessionId);
}