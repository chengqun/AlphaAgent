using AlphaAgent.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Domain.Interfaces;

public interface IVideoFeedRepository
{
    Task<List<VideoFeed>> GetPagedAsync(int limit, int offset);
    Task<VideoFeed?> GetByIdAsync(Guid id);
    Task<VideoFeed> AddAsync(VideoFeed videoFeed);
    Task<int> CountAsync();
}
