using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class MomentCacheRepository : IMomentCacheRepository
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public MomentCacheRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<MomentCacheItem>> GetAllAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.MomentCaches
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<DateTime?> GetLatestCreatedAtAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.MomentCaches
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (DateTime?)m.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddOrUpdateAsync(IEnumerable<MomentCacheItem> items)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        foreach (var item in items)
        {
            var existing = await dbContext.MomentCaches.FindAsync(item.Id);
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
                dbContext.MomentCaches.Add(item);
            }
        }
        await dbContext.SaveChangesAsync();
    }

    public async Task AddAsync(MomentCacheItem item)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var existing = await dbContext.MomentCaches.FindAsync(item.Id);
        if (existing != null) return;

        dbContext.MomentCaches.Add(item);
        await dbContext.SaveChangesAsync();
    }

    public async Task ClearAllAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.MomentCaches.RemoveRange(dbContext.MomentCaches);
        await dbContext.SaveChangesAsync();
    }
}