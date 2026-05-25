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
    private readonly SharesDbContext _dbContext;

    public MessageCacheRepository(SharesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetCachedMessagesJsonAsync(Guid conversationId)
    {
        // 1. 优先从内存读取
        if (_memoryCache.TryGetValue(conversationId, out var cachedJson))
        {
            System.Diagnostics.Debug.WriteLine($"[CacheRepository] 从内存缓存读取会话 {conversationId}");
            return cachedJson;
        }

        // 2. 内存没有，从 SQLite 读取（历史消息永久保留，不过期）
        System.Diagnostics.Debug.WriteLine($"[CacheRepository] 内存缓存未命中，尝试从 SQLite 读取会话 {conversationId}");
        var cacheItem = await _dbContext.MessageCache
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
        // 1. 更新内存缓存（同步）
        _memoryCache[conversationId] = messagesJson;
        System.Diagnostics.Debug.WriteLine($"[CacheRepository] 写入内存缓存，会话 {conversationId}，数据长度: {messagesJson.Length}");

        // 2. 异步写入 SQLite（持久化）
        await Task.Run(async () =>
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CacheRepository] 开始写入 SQLite，会话 {conversationId}");
                
                var existingItem = await _dbContext.MessageCache
                    .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

                if (existingItem != null)
                {
                    existingItem.MarkUpdated(messagesJson);
                    System.Diagnostics.Debug.WriteLine($"[CacheRepository] 更新 SQLite 中已存在的缓存，会话 {conversationId}");
                }
                else
                {
                    await _dbContext.MessageCache.AddAsync(new MessageCacheItem(conversationId, messagesJson));
                    System.Diagnostics.Debug.WriteLine($"[CacheRepository] 新建 SQLite 缓存记录，会话 {conversationId}");
                }

                await _dbContext.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"[CacheRepository] SQLite 写入成功，会话 {conversationId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CacheRepository] SQLite 写入失败，会话 {conversationId}: {ex.Message}");
            }
        });
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

        // 异步更新 SQLite
        await Task.Run(async () =>
        {
            try
            {
                var existingItem = await _dbContext.MessageCache
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
                        await _dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cache] SQLite append failed: {ex.Message}");
            }
        });
    }

    public async Task<DateTime?> GetLastCachedAtAsync(Guid conversationId)
    {
        var cacheItem = await _dbContext.MessageCache
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);
        return cacheItem?.LastCachedAt;
    }

    public async Task ClearCacheAsync(Guid conversationId)
    {
        _memoryCache.TryRemove(conversationId, out _);

        var cacheItem = await _dbContext.MessageCache
            .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

        if (cacheItem != null)
        {
            _dbContext.MessageCache.Remove(cacheItem);
            await _dbContext.SaveChangesAsync();
        }
    }

    public async Task ClearAllCacheAsync()
    {
        _memoryCache.Clear();
        _dbContext.MessageCache.RemoveRange(_dbContext.MessageCache);
        await _dbContext.SaveChangesAsync();
    }

}