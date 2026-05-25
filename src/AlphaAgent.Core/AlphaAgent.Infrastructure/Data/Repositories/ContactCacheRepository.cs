using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class ContactCacheRepository : IContactCacheRepository
{
    private readonly ConcurrentDictionary<Guid, ContactCacheItem> _memoryCache = new();
    private readonly SharesDbContext _dbContext;

    public ContactCacheRepository(SharesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ContactCacheItem>> GetAllAsync(Guid userId)
    {
        var cached = _memoryCache.Values.Where(c => c.UserId == userId).ToList();
        if (cached.Count > 0)
            return cached.OrderBy(c => c.Type).ThenBy(c => c.TargetName).ToList();

        var items = await _dbContext.ContactCache
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Type)
            .ThenBy(c => c.TargetName)
            .ToListAsync();

        foreach (var item in items)
            _memoryCache[item.Id] = item;

        return items;
    }

    public async Task UpsertRangeAsync(IEnumerable<ContactCacheItem> items)
    {
        var itemList = items.ToList();
        var now = DateTime.UtcNow;

        foreach (var item in itemList)
        {
            item.CachedAt = now;
            _memoryCache[item.Id] = item;
        }

        await Task.Run(async () =>
        {
            try
            {
                foreach (var item in itemList)
                {
                    var existing = await _dbContext.ContactCache.FindAsync(item.Id);
                    if (existing != null)
                    {
                        UpdateEntity(existing, item);
                    }
                    else
                    {
                        await _dbContext.ContactCache.AddAsync(item);
                    }
                }
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactCacheRepository] UpsertRangeAsync 失败: {ex.Message}");
            }
        });
    }

    public async Task DeleteAllAsync(Guid userId)
    {
        foreach (var key in _memoryCache.Where(kvp => kvp.Value.UserId == userId).Select(kvp => kvp.Key).ToList())
            _memoryCache.TryRemove(key, out _);

        await Task.Run(async () =>
        {
            try
            {
                var items = await _dbContext.ContactCache
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
                _dbContext.ContactCache.RemoveRange(items);
                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ContactCacheRepository] DeleteAllAsync 失败: {ex.Message}");
            }
        });
    }

    public async Task<DateTime?> GetLastCachedAtAsync(Guid userId)
    {
        var memoryMax = _memoryCache.Values
            .Where(c => c.UserId == userId)
            .Max(c => (DateTime?)c.CachedAt);

        if (memoryMax.HasValue)
            return memoryMax;

        return await _dbContext.ContactCache
            .Where(c => c.UserId == userId)
            .MaxAsync(c => (DateTime?)c.CachedAt);
    }

    private static void UpdateEntity(ContactCacheItem existing, ContactCacheItem source)
    {
        existing.Type = source.Type;
        existing.TargetId = source.TargetId;
        existing.TargetName = source.TargetName;
        existing.DeviceType = source.DeviceType;
        existing.Status = source.Status;
        existing.CachedAt = source.CachedAt;
        existing.UserId = source.UserId;
    }
}
