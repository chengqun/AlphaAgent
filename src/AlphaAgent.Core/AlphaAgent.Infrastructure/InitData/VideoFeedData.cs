using AlphaAgent.Domain.Entities;

namespace AlphaAgent.Infrastructure.InitData;

public static class VideoFeedData
{
    public static readonly VideoFeed[] All =
    [
        new VideoFeed(
            "聚宝盆",
            "https://vodcnd01.myqqdd.com/20260511/noPZHlna/2619kb/hls/index.m3u8"
        ),
        new VideoFeed(
            "修仙不如干饭",
            "https://play.modujx11.com/20260502/5EQ8k4ac/index.m3u8"
        ),
    ];
}
