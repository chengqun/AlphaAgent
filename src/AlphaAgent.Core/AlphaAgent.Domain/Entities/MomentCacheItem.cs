using System;

namespace AlphaAgent.Domain.Entities;

public class MomentCacheItem
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "Text";
    public string Visibility { get; set; } = "Friends";
}
