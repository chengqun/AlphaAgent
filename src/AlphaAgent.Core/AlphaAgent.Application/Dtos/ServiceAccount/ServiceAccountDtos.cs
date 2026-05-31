using System;

namespace AlphaAgent.Application.Dtos.ServiceAccount;

/// <summary>
/// 服务号 DTO
/// </summary>
public class ServiceAccountDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? WelcomeMessage { get; set; }
    public DateTime CreationTime { get; set; }
    public int FollowerCount { get; set; }
    public bool IsFollowedByCurrentUser { get; set; }
}

/// <summary>
/// 服务号内容 DTO（完整版）
/// </summary>
public class ServiceAccountPostDto
{
    public Guid Id { get; set; }
    public Guid ServiceAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImageUrl { get; set; }
    public string ContentType { get; set; } = "Article";
    public bool IsPinned { get; set; }
    public DateTime PublishedAt { get; set; }
    public string ServiceAccountName { get; set; } = string.Empty;
    public string? ServiceAccountAvatarUrl { get; set; }
}

/// <summary>
/// 服务号内容列表项 DTO
/// </summary>
public class ServiceAccountPostListItemDto
{
    public Guid Id { get; set; }
    public Guid ServiceAccountId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? CoverImageUrl { get; set; }
    public string ContentType { get; set; } = "Article";
    public bool IsPinned { get; set; }
    public DateTime PublishedAt { get; set; }
    public string ServiceAccountName { get; set; } = string.Empty;
}