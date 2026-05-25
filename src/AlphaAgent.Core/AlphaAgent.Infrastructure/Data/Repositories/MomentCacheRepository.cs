using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class MomentCacheRepository : IMomentCacheRepository
{
    private readonly SharesDbContext _dbContext;

    public MomentCacheRepository(SharesDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<MomentCacheItem>> GetAllAsync()
    {
        return await _dbContext.MomentCaches
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<DateTime?> GetLatestCreatedAtAsync()
    {
        return await _dbContext.MomentCaches
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (DateTime?)m.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddOrUpdateAsync(IEnumerable<MomentCacheItem> items)
    {
        foreach (var item in items)
        {
            var existing = await _dbContext.MomentCaches.FindAsync(item.Id);
            if (existing != null)
            {
                existing.UserId = item.UserId;
                existing.Username = item.Username;
                existing.Content = item.Content;
                existing.ImageUrl = item.ImageUrl;
                existing.CreatedAt = item.CreatedAt;
                existing.Type = item.Type;
                existing.Visibility = item.Visibility;
            }
            else
            {
                _dbContext.MomentCaches.Add(item);
            }
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddAsync(MomentCacheItem item)
    {
        var existing = await _dbContext.MomentCaches.FindAsync(item.Id);
        if (existing != null) return;

        _dbContext.MomentCaches.Add(item);
        await _dbContext.SaveChangesAsync();
    }

    public async Task ClearAllAsync()
    {
        _dbContext.MomentCaches.RemoveRange(_dbContext.MomentCaches);
        await _dbContext.SaveChangesAsync();
    }
}
