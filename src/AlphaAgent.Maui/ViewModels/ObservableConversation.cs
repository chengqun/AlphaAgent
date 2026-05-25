using CommunityToolkit.Mvvm.ComponentModel;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Domain.Abstractions.Chat;
using System;

namespace AlphaAgent.Maui.ViewModels;

public partial class ObservableConversation : ObservableObject
{
    private readonly Conversation _source;

    public ObservableConversation(Conversation source)
    {
        _source = source;
        UnreadCount = source.UnreadCount;
        LastMessage = source.LastMessage;
        LastMessageTime = source.LastMessageTime;
    }

    public Guid Id => _source.Id;
    public int Type => _source.Type;
    public string? Name => _source.Name;
    public string? OtherUserName => _source.OtherUserName;
    public Guid? OtherUserId => _source.OtherUserId;
    public string? OtherDeviceId => _source.OtherDeviceId;
    public string? DeviceType => _source.DeviceType;
    public int MemberCount => _source.MemberCount;
    public string? Context => _source.Context;

    [ObservableProperty]
    private int _unreadCount;

    [ObservableProperty]
    private string? _lastMessage;

    [ObservableProperty]
    private DateTime? _lastMessageTime;

    public string DisplayName => _source.DisplayName;
    public string Initial => _source.Initial;
    public string? IconSource => _source.IconSource;

    public void UpdateFromMessage(ChatMessage message)
    {
        UnreadCount++;
        LastMessage = message.Content;
        LastMessageTime = message.SentAt;
    }

    public void ClearUnreadCount()
    {
        UnreadCount = 0;
    }

    public void AddUnreadMessages(int count)
    {
        UnreadCount += count;
    }

    public void UpdateFromSource(Conversation source)
    {
        // 更新源数据
        _source.UnreadCount = source.UnreadCount;
        _source.LastMessage = source.LastMessage;
        _source.LastMessageTime = source.LastMessageTime;
        
        // 更新可观察属性
        UnreadCount = source.UnreadCount;
        LastMessage = source.LastMessage;
        LastMessageTime = source.LastMessageTime;
    }
}