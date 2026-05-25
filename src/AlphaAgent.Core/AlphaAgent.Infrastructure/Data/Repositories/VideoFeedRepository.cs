using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Data.Repositories;

public class VideoFeedRepository : IVideoFeedRepository
{
    private readonly SharesDbContext _context;

    public VideoFeedRepository(SharesDbContext context)
    {
        _context = context;
    }

    public async Task<List<VideoFeed>> GetPagedAsync(int limit, int offset)
    {
        return await _context.VideoFeeds
            .OrderByDescending(v => v.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<VideoFeed?> GetByIdAsync(Guid id)
    {
        return await _context.VideoFeeds.FindAsync(id);
    }

    public async Task<VideoFeed> AddAsync(VideoFeed videoFeed)
    {
        _context.VideoFeeds.Add(videoFeed);
        await _context.SaveChangesAsync();
        return videoFeed;
    }

    public async Task<int> CountAsync()
    {
        return await _context.VideoFeeds.CountAsync();
    }
}
