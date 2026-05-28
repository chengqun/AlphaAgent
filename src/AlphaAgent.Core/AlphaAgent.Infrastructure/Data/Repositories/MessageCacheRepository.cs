using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class MessageCacheRepository : IMessageCacheRepository
{
    private readonly ConcurrentDictionary<Guid, string> _memoryCache = new();
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public MessageCacheRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<string?> GetCachedMessagesJsonAsync(Guid conversationId)
    {
        // 1. 优先从内存读取
        if (_memoryCache.TryGetValue(conversationId, out var cachedJson))
        {
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] 从内存缓存读取会话 {conversationId}");
            return cachedJson;
        }

        // 2. 内存没有，从 SQLite 读取
        System.Diagnostics.Debug.WriteLine($"[CacheRepository] 内存缓存未命中，尝试从 SQLite 读取会话 {conversationId}");
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var cacheItem = await dbContext.MessageCache
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (cacheItem == null)
        {
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] SQLite 中未找到会话 {conversationId} 的缓存");
            return null;
        }

        // 加载到内存缓存
        _memoryCache[conversationId] = cacheItem.MessageData;
        System.Diagnostics.Debug.WriteLine($"[CacheRepository] 从 SQLite 读取会话 {conversationId} 的缓存，数据长度: {cacheItem.MessageData.Length}");

        return cacheItem.MessageData;
    }

    public async Task CacheMessagesJsonAsync(Guid conversationId, string messagesJson)
    {
        // 1. 更新内存缓存
        _memoryCache[conversationId] = messagesJson;
        System.Diagnostics.Debug.WriteLine($"[CacheRepository] 写入内存缓存，会话 {conversationId}，数据长度: {messagesJson.Length}");

        // 2. 写入 SQLite
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] 开始写入 SQLite，会话 {conversationId}");

            var existingItem = await dbContext.MessageCache
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (existingItem != null)
            {
                existingItem.MarkUpdated(messagesJson);
                System.Diagnostics.Debug.WriteLine($"[CacheRepository] 更新 SQLite 中已存在的缓存，会话 {conversationId}");
            }
            else
            {
                await dbContext.MessageCache.AddAsync(new MessageCacheItem(conversationId, messagesJson));
                System.Diagnostics.Debug.WriteLine($"[CacheRepository] 新建 SQLite 缓存记录，会话 {conversationId}");
            }

            await dbContext.SaveChangesAsync();
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] SQLite 写入成功，会话 {conversationId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] SQLite 写入失败，会话 {conversationId}: {ex.Message}");
        }
    }

    public async Task AppendMessageJsonAsync(Guid conversationId, string messageJson)
    {
        // 更新内存缓存
        if (_memoryCache.TryGetValue(conversationId, out var existingJson))
        {
            if (!string.IsNullOrEmpty(existingJson) &&
                existingJson.StartsWith("[") &&
                existingJson.EndsWith("]"))
            {
                var newJson = existingJson.Substring(0, existingJson.Length - 1) +
                              "," + messageJson + "]";
                _memoryCache[conversationId] = newJson;
            }
            else
            {
                _memoryCache[conversationId] = $"[{messageJson}]";
            }
        }
        else
        {
            _memoryCache[conversationId] = $"[{messageJson}]";
        }

        // 更新 SQLite
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var existingItem = await dbContext.MessageCache
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (existingItem != null)
            {
                if (!string.IsNullOrEmpty(existingItem.MessageData) &&
                    existingItem.MessageData.StartsWith("[") &&
                    existingItem.MessageData.EndsWith("]"))
                {
                    var newJson = existingItem.MessageData.Substring(0, existingItem.MessageData.Length - 1) +
                                  "," + messageJson + "]";
                    existingItem.MarkUpdated(newJson);
                    await dbContext.SaveChangesAsync();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Cache] SQLite append failed: {ex.Message}");
        }
    }

    public async Task<DateTime?> GetLastCachedAtAsync(Guid conversationId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var cacheItem = await dbContext.MessageCache
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        return cacheItem?.LastCachedAt;
    }

    public async Task ClearCacheAsync(Guid conversationId)
    {
        _memoryCache.TryRemove(conversationId, out _);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var cacheItem = await dbContext.MessageCache
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (cacheItem != null)
        {
            dbContext.MessageCache.Remove(cacheItem);
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task ClearAllCacheAsync()
    {
        _memoryCache.Clear();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.MessageCache.RemoveRange(dbContext.MessageCache);
        await dbContext.SaveChangesAsync();
    }

}
