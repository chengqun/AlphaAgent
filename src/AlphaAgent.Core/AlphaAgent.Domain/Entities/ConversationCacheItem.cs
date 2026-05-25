using System;

namespace AlphaAgent.Domain.Entities;

public class ConversationCacheItem
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public string? Name { get; set; }
    public string? OtherUserName { get; set; }
    public Guid? OtherUserId { get; set; }
    public string? OtherDeviceId { get; set; }
    public string? DeviceType { get; set; }
    public int UnreadCount { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int MemberCount { get; set; }
    public string? Context { get; set; }
    public DateTime CachedAt { get; set; }
    public Guid UserId { get; set; }
}
