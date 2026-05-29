using System;

namespace AlphaAgent.Application.Dtos.Chat;

public class Conversation
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

    public string DisplayName => Type == 0
        ? (OtherUserName ?? Name ?? "未知用户")
        : (Name ?? "群聊");

    public string Initial
    {
        get
        {
            var name = DisplayName;
            return string.IsNullOrEmpty(name) ? "?" : name[..1].ToUpperInvariant();
        }
    }

    public string? IconSource
    {
        get
        {
            if (Type == 0 && !string.IsNullOrEmpty(DeviceType))
            {
                if (DeviceType.Contains("windows", StringComparison.OrdinalIgnoreCase))
                    return "windows";
                if (DeviceType.Contains("macos", StringComparison.OrdinalIgnoreCase))
                    return "macos";
                if (DeviceType.Contains("claude-bridge", StringComparison.OrdinalIgnoreCase))
                    return "claude_bridge";
            }
            return null;
        }
    }
}

public class UnreadCountResult
{
    public int Count { get; set; }
}
