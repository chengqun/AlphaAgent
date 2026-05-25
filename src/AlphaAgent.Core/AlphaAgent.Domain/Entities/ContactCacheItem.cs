using System;

namespace AlphaAgent.Domain.Entities;

/// <summary>
/// 通讯录本地缓存：每个联系人一条记录，Category 区分好友/群组/设备/股票
/// </summary>
public class ContactCacheItem
{
    public Guid Id { get; set; }              // RelationshipDto.Id
    public int Type { get; set; }             // RelationshipType enum value
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public int Status { get; set; }
    public DateTime CachedAt { get; set; }
    public Guid UserId { get; set; }         // 区分不同登录用户
}
