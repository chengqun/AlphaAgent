using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class AgentConfigCacheRepository : IAgentConfigCacheRepository
{
    private readonly ConcurrentDictionary<Guid, AgentConfigCacheItem> _memoryCache = new();
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public AgentConfigCacheRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<AgentConfigCacheItem>> GetByUserIdAsync(Guid userId)
    {
        var cached = _memoryCache.Values.Where(c => c.UserId == userId).ToList();
        if (cached.Count > 0)
            return cached.OrderByDescending(c => c.IsActive).ThenBy(c => c.AgentName).ToList();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var items = await dbContext.AgentConfigCache
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsActive)
            .ThenBy(c => c.AgentName)
            .ToListAsync();

        foreach (var item in items)
        {
            item.DeserializeEnabledTools();
            _memoryCache[item.Id] = item;
        }

        return items;
    }

    public async Task UpsertRangeAsync(IEnumerable<AgentConfigCacheItem> items)
    {
        var itemList = items.ToList();
        var now = DateTime.UtcNow;

        foreach (var item in itemList)
        {
            item.CachedAt = now;
            item.SerializeEnabledTools();
            _memoryCache[item.Id] = item;
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            foreach (var item in itemList)
            {
                var existing = await dbContext.AgentConfigCache.FindAsync(item.Id);
                if (existing != null)
                {
                    UpdateEntity(existing, item);
                }
                else
                {
                    await dbContext.AgentConfigCache.AddAsync(item);
                }
            }
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentConfigCacheRepository] UpsertRangeAsync 失败: {ex.Message}");
        }
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        foreach (var key in _memoryCache.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList())
            _memoryCache.TryRemove(key, out _);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var items = await dbContext.AgentConfigCache
                .Where(c => c.UserId == userId)
                .ToListAsync();
            dbContext.AgentConfigCache.RemoveRange(items);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentConfigCacheRepository] DeleteByUserIdAsync 失败: {ex.Message}");
        }
    }

    private static void UpdateEntity(AgentConfigCacheItem existing, AgentConfigCacheItem source)
    {
        existing.AgentName = source.AgentName;
        existing.ModelName = source.ModelName;
        existing.ApiKey = source.ApiKey;
        existing.Endpoint = source.Endpoint;
        existing.DefaultSystemPrompt = source.DefaultSystemPrompt;
        existing.Temperature = source.Temperature;
        existing.IsActive = source.IsActive;
        existing.EnabledToolsJson = source.EnabledToolsJson;
        existing.CachedAt = source.CachedAt;
        existing.UserId = source.UserId;
    }
}
