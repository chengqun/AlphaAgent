using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class LlmConfigCacheRepository : ILlmConfigCacheRepository
{
    private readonly ConcurrentDictionary<Guid, LlmConfigCacheItem> _memoryCache = new();
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public LlmConfigCacheRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<LlmConfigCacheItem>> GetByUserIdAsync(Guid userId)
    {
        var cached = _memoryCache.Values.Where(c => c.UserId == userId).ToList();
        if (cached.Count > 0)
            return cached.OrderByDescending(c => c.IsDefault).ThenBy(c => c.Name).ToList();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var items = await dbContext.LlmConfigCache
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IsDefault)
            .ThenBy(c => c.Name)
            .ToListAsync();

        foreach (var item in items)
            _memoryCache[item.Id] = item;

        return items;
    }

    public async Task UpsertRangeAsync(IEnumerable<LlmConfigCacheItem> items)
    {
        var itemList = items.ToList();
        var now = DateTime.UtcNow;

        foreach (var item in itemList)
        {
            item.CachedAt = now;
            _memoryCache[item.Id] = item;
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            foreach (var item in itemList)
            {
                var existing = await dbContext.LlmConfigCache.FindAsync(item.Id);
                if (existing != null)
                {
                    UpdateEntity(existing, item);
                }
                else
                {
                    await dbContext.LlmConfigCache.AddAsync(item);
                }
            }
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LlmConfigCacheRepository] UpsertRangeAsync 失败: {ex.Message}");
        }
    }

    public async Task DeleteByUserIdAsync(Guid userId)
    {
        foreach (var key in _memoryCache.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList())
            _memoryCache.TryRemove(key, out _);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var items = await dbContext.LlmConfigCache
                .Where(c => c.UserId == userId)
                .ToListAsync();
            dbContext.LlmConfigCache.RemoveRange(items);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LlmConfigCacheRepository] DeleteByUserIdAsync 失败: {ex.Message}");
        }
    }

    private static void UpdateEntity(LlmConfigCacheItem existing, LlmConfigCacheItem source)
    {
        existing.Name = source.Name;
        existing.ModelName = source.ModelName;
        existing.ApiKey = source.ApiKey;
        existing.Endpoint = source.Endpoint;
        existing.Temperature = source.Temperature;
        existing.IsDefault = source.IsDefault;
        existing.CachedAt = source.CachedAt;
        existing.UserId = source.UserId;
    }
}
