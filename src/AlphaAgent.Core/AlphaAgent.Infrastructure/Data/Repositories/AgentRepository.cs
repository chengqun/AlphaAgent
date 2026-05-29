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
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public AgentRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<AgentSession> CreateSessionAsync(Guid userId, string agentName)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var session = new AgentSession(userId, agentName);
        dbContext.AgentSessions.Add(session);
        await dbContext.SaveChangesAsync();
        return session;
    }

    public async Task<AgentSession?> GetSessionAsync(Guid sessionId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentSessions
            .Include(s => s.Messages)
            .FirstOrDefaultAsync(s => s.Id == sessionId);
    }

    public async Task<AgentSession?> GetActiveSessionAsync(Guid userId, string agentName)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentSessions
            .Where(s => s.UserId == userId && s.Status == AgentSessionStatus.Active && s.AgentName == agentName
                        && (s.Context == null || s.Context == ""))
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<AgentSession?> GetActiveSessionByContextAsync(Guid userId, string agentName, string context)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentSessions
            .Where(s => s.UserId == userId && s.Status == AgentSessionStatus.Active && s.AgentName == agentName && s.Context == context)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<AgentSession>> GetUserSessionsAsync(Guid userId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentSessions
            .Include(s => s.Messages)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateSessionAsync(AgentSession session)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // 更新 session 实体本身（Context、Status 等标量属性）
        var existingSession = await dbContext.AgentSessions.FindAsync(session.Id);
        if (existingSession != null)
        {
            existingSession.Context = session.Context;
            existingSession.Status = session.Status;
        }

        // 获取数据库中已有的消息ID
        var existingMessageIds = await dbContext.AgentMessages
            .Where(m => m.SessionId == session.Id)
            .Select(m => m.Id)
            .ToListAsync();

        // 只添加新消息
        foreach (var message in session.Messages)
        {
            if (!existingMessageIds.Contains(message.Id))
            {
                dbContext.AgentMessages.Add(message);
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteSessionAsync(Guid sessionId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var session = await dbContext.AgentSessions.FindAsync(sessionId);
        if (session != null)
        {
            dbContext.AgentSessions.Remove(session);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task AddMessageAsync(AgentMessage message)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.AgentMessages.Add(message);
        await dbContext.SaveChangesAsync();
    }

    public async Task<List<AgentMessage>> GetSessionMessagesAsync(Guid sessionId, int? limit = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var query = dbContext.AgentMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp);

        if (limit.HasValue)
            query = (IOrderedQueryable<AgentMessage>)query.Take(limit.Value);

        return await query.OrderBy(m => m.Timestamp).ToListAsync();
    }

    public async Task<AgentTask> CreateTaskAsync(Guid sessionId, string taskType, string? input = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var task = new AgentTask(sessionId, taskType, input);
        dbContext.AgentTasks.Add(task);
        await dbContext.SaveChangesAsync();
        return task;
    }

    public async Task<AgentTask?> GetTaskAsync(Guid taskId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentTasks.FindAsync(taskId);
    }

    public async Task<List<AgentTask>> GetSessionTasksAsync(Guid sessionId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.AgentTasks
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateTaskAsync(AgentTask task)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.AgentTasks.Update(task);
        await dbContext.SaveChangesAsync();
    }
}
