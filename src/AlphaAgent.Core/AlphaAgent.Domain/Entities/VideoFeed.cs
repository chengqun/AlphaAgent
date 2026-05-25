using System;

namespace AlphaAgent.Domain.Entities;

public class VideoFeed
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string VideoUrl { get; private set; } = string.Empty;
    public string? CoverUrl { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public int Duration { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private VideoFeed() { }

    public VideoFeed(string title, string videoUrl, string? author = null, int duration = 0, string? coverUrl = null)
    {
        Id = Guid.NewGuid();
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required", nameof(title)) : title;
        VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? throw new ArgumentException("VideoUrl is required", nameof(videoUrl)) : videoUrl;
        Author = author ?? string.Empty;
        Duration = duration < 0 ? throw new ArgumentException("Duration must be non-negative", nameof(duration)) : duration;
        CoverUrl = coverUrl;
        CreatedAt = DateTime.UtcNow;
    }
}
