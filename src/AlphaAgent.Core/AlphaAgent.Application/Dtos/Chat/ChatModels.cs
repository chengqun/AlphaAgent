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

    /// <summary>
    /// Agent 会话的 Agent 名称（用于恢复会话时锁定 Agent，不从全局设置读取）。
    /// 仅 Type 3/4 有值。
    /// </summary>
    public string? AgentName { get; set; }

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

    /// <summary>
    /// Agent 会话的头像 emoji：Type 3 显示 🤖，Type 4 显示 📈，其他为 null。
    /// </summary>
    public string? AgentAvatar
    {
        get
        {
            if (Type == 3) return "🤖";
            if (Type == 4) return "📈";
            return null;
        }
    }

    /// <summary>
    /// 是否显示字母头像（没有 IconSource 也没有 AgentAvatar 时）
    /// </summary>
    public bool ShowLetterAvatar => IconSource == null && AgentAvatar == null;
}

public class UnreadCountResult
{
    public int Count { get; set; }
}
