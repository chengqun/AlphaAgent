using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class ConversationCacheRepository : IConversationCacheRepository
{
    private readonly ConcurrentDictionary<Guid, ConversationCacheItem> _memoryCache = new();
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public ConversationCacheRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<ConversationCacheItem>> GetAllAsync(Guid userId)
    {
        var cached = _memoryCache.Values.Where(c => c.UserId == userId).ToList();
        if (cached.Count > 0)
            return cached.OrderByDescending(c => c.LastMessageTime).ToList();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var items = await dbContext.ConversationCache
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.LastMessageTime)
            .ToListAsync();

        foreach (var item in items)
            _memoryCache[item.Id] = item;

        return items;
    }

    public async Task UpsertAsync(ConversationCacheItem item)
    {
        item.CachedAt = DateTime.UtcNow;
        _memoryCache[item.Id] = item;

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.ConversationCache.FindAsync(item.Id);
            if (existing != null)
            {
                UpdateEntity(existing, item);
                await dbContext.SaveChangesAsync();
            }
            else
            {
                await dbContext.ConversationCache.AddAsync(item);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConversationCacheRepository] UpsertAsync 失败: {ex.Message}");
        }
    }

    public async Task UpsertRangeAsync(IEnumerable<ConversationCacheItem> items)
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
                var existing = await dbContext.ConversationCache.FindAsync(item.Id);
                if (existing != null)
                {
                    UpdateEntity(existing, item);
                }
                else
                {
                    await dbContext.ConversationCache.AddAsync(item);
                }
            }
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConversationCacheRepository] UpsertRangeAsync 失败: {ex.Message}");
        }
    }

    public async Task DeleteAsync(Guid conversationId)
    {
        _memoryCache.TryRemove(conversationId, out _);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var existing = await dbContext.ConversationCache.FindAsync(conversationId);
            if (existing != null)
            {
                dbContext.ConversationCache.Remove(existing);
                await dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConversationCacheRepository] DeleteAsync 失败: {ex.Message}");
        }
    }

    public async Task DeleteAllAsync(Guid userId)
    {
        foreach (var key in _memoryCache.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList())
            _memoryCache.TryRemove(key, out _);

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var items = await dbContext.ConversationCache
                .Where(c => c.UserId == userId)
                .ToListAsync();
            dbContext.ConversationCache.RemoveRange(items);
            await dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ConversationCacheRepository] DeleteAllAsync 失败: {ex.Message}");
        }
    }

    public async Task<DateTime?> GetLastCachedAtAsync(Guid userId)
    {
        var memoryMax = _memoryCache.Values
            .Where(c => c.UserId == userId)
            .Max(c => (DateTime?)c.CachedAt);

        if (memoryMax.HasValue)
            return memoryMax;

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.ConversationCache
            .Where(c => c.UserId == userId)
            .MaxAsync(c => (DateTime?)c.CachedAt);
    }

    private static void UpdateEntity(ConversationCacheItem existing, ConversationCacheItem source)
    {
        existing.Type = source.Type;
        existing.Name = source.Name;
        existing.OtherUserName = source.OtherUserName;
        existing.OtherUserId = source.OtherUserId;
        existing.OtherDeviceId = source.OtherDeviceId;
        existing.DeviceType = source.DeviceType;
        existing.UnreadCount = source.UnreadCount;
        existing.LastMessage = source.LastMessage;
        existing.LastMessageTime = source.LastMessageTime;
        existing.MemberCount = source.MemberCount;
        existing.Context = source.Context;
        existing.CachedAt = source.CachedAt;
        existing.UserId = source.UserId;
    }
}
