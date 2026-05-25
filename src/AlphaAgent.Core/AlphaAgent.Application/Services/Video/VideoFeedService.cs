using AlphaAgent.Application.Interfaces.Video;
using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Video;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Video;

public class VideoFeedService : IVideoFeedService
{
    private readonly IVideoFeedRepository _videoFeedRepository;

    public VideoFeedService(IVideoFeedRepository videoFeedRepository)
    {
        _videoFeedRepository = videoFeedRepository;
    }

    public async Task<ApiResponse<List<VideoItemDto>>> GetVideoFeedAsync(int limit = 20, int offset = 0)
    {
        var videoFeeds = await _videoFeedRepository.GetPagedAsync(limit, offset);
        var dtos = videoFeeds.Select(ToDto).ToList();
        return new ApiResponse<List<VideoItemDto>> { Success = true, Data = dtos };
    }

    public async Task<ApiResponse<List<VideoItemDto>>> GetMoreVideosAsync(int limit = 20, int offset = 0)
    {
        var videoFeeds = await _videoFeedRepository.GetPagedAsync(limit, offset);
        var dtos = videoFeeds.Select(ToDto).ToList();
        return new ApiResponse<List<VideoItemDto>> { Success = true, Data = dtos };
    }

    public async Task<ApiResponse<VideoItemDto>> AddVideoAsync(VideoItemDto videoItem)
    {
        var videoFeed = new VideoFeed(
            videoItem.Title,
            videoItem.VideoUrl,
            videoItem.Author,
            videoItem.Duration,
            videoItem.CoverUrl
        );
        var created = await _videoFeedRepository.AddAsync(videoFeed);
        return new ApiResponse<VideoItemDto> { Success = true, Data = ToDto(created) };
    }

    private static VideoItemDto ToDto(VideoFeed entity) => new()
    {
        Id = entity.Id,
        Title = entity.Title,
        VideoUrl = entity.VideoUrl,
        CoverUrl = entity.CoverUrl,
        Author = entity.Author,
        Duration = entity.Duration,
        CreatedAt = entity.CreatedAt
    };
}
