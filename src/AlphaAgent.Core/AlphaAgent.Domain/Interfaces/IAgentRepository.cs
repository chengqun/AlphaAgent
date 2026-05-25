using AlphaAgent.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface IAgentRepository
{
    Task<AgentSession> CreateSessionAsync(Guid userId, string agentName);
    Task<AgentSession?> GetSessionAsync(Guid sessionId);
    Task<AgentSession?> GetActiveSessionAsync(Guid userId, string agentName);
    Task<AgentSession?> GetActiveSessionByContextAsync(Guid userId, string agentName, string context);
    Task<List<AgentSession>> GetUserSessionsAsync(Guid userId);
    Task UpdateSessionAsync(AgentSession session);
    Task DeleteSessionAsync(Guid sessionId);

    Task AddMessageAsync(AgentMessage message);
    Task<List<AgentMessage>> GetSessionMessagesAsync(Guid sessionId, int? limit = null);

    Task<AgentTask> CreateTaskAsync(Guid sessionId, string taskType, string? input = null);
    Task<AgentTask?> GetTaskAsync(Guid taskId);
    Task<List<AgentTask>> GetSessionTasksAsync(Guid sessionId);
    Task UpdateTaskAsync(AgentTask task);
}