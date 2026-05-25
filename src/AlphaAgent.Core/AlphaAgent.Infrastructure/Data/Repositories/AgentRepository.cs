using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class AgentRepository : IAgentRepository
{
    private readonly SharesDbContext _dbContext;

    public AgentRepository(SharesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AgentSession> CreateSessionAsync(Guid userId, string agentName)
    {
        var session = new AgentSession(userId, agentName);
        _dbContext.AgentSessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<AgentSession?> GetSessionAsync(Guid sessionId)
    {
        return await _dbContext.AgentSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<AgentSession?> GetActiveSessionAsync(Guid userId, string agentName)
    {
        // 不加载消息，提高查询性能
        // 排除带 Context 的会话（如股票会话），它们通过 GetActiveSessionByContextAsync 查找
        return await _dbContext.AgentSessions
            .Where(s => s.UserId == userId && s.Status == AgentSessionStatus.Active && s.AgentName == agentName
                        && (s.Context == null || s.Context == ""))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AgentSession?> GetActiveSessionByContextAsync(Guid userId, string agentName, string context)
    {
        return await _dbContext.AgentSessions
            .Where(s => s.UserId == userId && s.Status == AgentSessionStatus.Active && s.AgentName == agentName && s.Context == context)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AgentSession>> GetUserSessionsAsync(Guid userId)
    {
        return await _dbContext.AgentSessions
            .Include(s => s.Messages)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateSessionAsync(AgentSession session)
    {
        // 获取数据库中已有的消息ID
        var existingMessageIds = await _dbContext.AgentMessages
            .Where(m => m.SessionId == session.Id)
            .Select(m => m.Id)
            .ToListAsync();
        
        // 只添加新消息
        foreach (var message in session.Messages)
        {
            if (!existingMessageIds.Contains(message.Id))
            {
                _dbContext.AgentMessages.Add(message);
            }
        }
        
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        var session = await _dbContext.AgentSessions.FindAsync(sessionId);
        if (session != null)
        {
            _dbContext.AgentSessions.Remove(session);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task AddMessageAsync(AgentMessage message)
    {
        _dbContext.AgentMessages.Add(message);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<AgentMessage>> GetSessionMessagesAsync(Guid sessionId, int? limit = null)
    {
        var query = _dbContext.AgentMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp);

        if (limit.HasValue)
            query = (IOrderedQueryable<AgentMessage>)query.Take(limit.Value);

        return await query.OrderBy(m => m.Timestamp).ToListAsync();
    }

    public async Task<AgentTask> CreateTaskAsync(Guid sessionId, string taskType, string? input = null)
    {
        var task = new AgentTask(sessionId, taskType, input);
        _dbContext.AgentTasks.Add(task);
        await _dbContext.SaveChangesAsync();
        return task;
    }

    public async Task<AgentTask?> GetTaskAsync(Guid taskId)
    {
        return await _dbContext.AgentTasks.FindAsync(taskId);
    }

    public async Task<List<AgentTask>> GetSessionTasksAsync(Guid sessionId)
    {
        return await _dbContext.AgentTasks
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateTaskAsync(AgentTask task)
    {
        _dbContext.AgentTasks.Update(task);
        await _dbContext.SaveChangesAsync();
    }
}