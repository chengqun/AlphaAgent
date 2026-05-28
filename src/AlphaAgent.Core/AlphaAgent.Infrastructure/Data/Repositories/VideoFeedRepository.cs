using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class VideoFeedRepository : IVideoFeedRepository
{
    private readonly IDbContextFactory<SharesDbContext> _dbContextFactory;

    public VideoFeedRepository(IDbContextFactory<SharesDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<VideoFeed>> GetPagedAsync(int limit, int offset)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.VideoFeeds
            .OrderByDescending(v => v.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<VideoFeed?> GetByIdAsync(Guid id)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.VideoFeeds.FindAsync(id);
    }

    public async Task<VideoFeed> AddAsync(VideoFeed videoFeed)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        dbContext.VideoFeeds.Add(videoFeed);
        await dbContext.SaveChangesAsync();
        return videoFeed;
    }

    public async Task<int> CountAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.VideoFeeds.CountAsync();
    }
}