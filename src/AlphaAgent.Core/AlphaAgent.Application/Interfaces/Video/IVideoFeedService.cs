using AlphaAgent.Application.Dtos.Common;
using AlphaAgent.Application.Dtos.Video;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Interfaces.Video;

public interface IVideoFeedService
{
    Task<ApiResponse<List<VideoItemDto>>> GetVideoFeedAsync(int limit = 20, int offset = 0);
    Task<ApiResponse<List<VideoItemDto>>> GetMoreVideosAsync(int limit = 20, int offset = 0);
    Task<ApiResponse<VideoItemDto>> AddVideoAsync(VideoItemDto videoItem);
}
