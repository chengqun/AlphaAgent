using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Maui.Services;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Abstractions.Chat;
using AlphaAgent.Domain.Services.Auth;
using AlphaAgent.Maui.Events;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlphaAgent.Maui.ViewModels;

public partial class ChatDetailViewModel : ObservableObject, IQueryAttributable, IPageLifecycleAware
{
    private readonly IAuthService? _authService;
    private readonly IChatService? _chatService;
    private readonly ISignalRChatService? _signalRChatService;
    private readonly IMessageCacheService? _messageCacheService;
    private readonly IEventBusService? _eventBusService;
    private readonly ITokenManager? _tokenManager;
    private readonly IUnreadMessageCacheService? _unreadMessageCacheService;
    private Guid _currentConversationId;
    private Guid _currentUserId;

    // 每次切换会话递增，用于丢弃过期的异步操作结果
    private int _loadVersion;

    // 防止 OnAppearingAsync 并发执行
    private volatile bool _isAppearing;

    // 缓存 userId，避免每次打开聊天页都重新解析 JWT
    private static Guid? _cachedUserId;

    // 从通讯录导航时，会话尚未创建，需在 ChatDetail 内部创建
    [ObservableProperty]
    private string _pendingContactId = string.Empty;

    [ObservableProperty]
    private string _pendingContactType = string.Empty;

    [ObservableProperty]
    private string _conversationId = string.Empty;

    [ObservableProperty]
    private string _conversationName = "聊天";

    [ObservableProperty]
    private int _conversationType;

    [ObservableProperty]
    private string _messageInput = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<ChatMessage> Messages { get; } = new();
    private readonly HashSet<Guid> _displayedMessageIds = new();
    private DateTime _lastMessageTime;

    public ChatDetailViewModel(IAuthService? authService = null,
                               IChatService? chatService = null,
                               ISignalRChatService? signalRChatService = null,
                               IMessageCacheService? messageCacheService = null,
                               IEventBusService? eventBusService = null,
                               ITokenManager? tokenManager = null,
                               IUnreadMessageCacheService? unreadMessageCacheService = null)
    {
        _authService = authService;
        _chatService = chatService;
        _signalRChatService = signalRChatService;
        _messageCacheService = messageCacheService;
        _eventBusService = eventBusService;
        _tokenManager = tokenManager;
        _unreadMessageCacheService = unreadMessageCacheService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("conversationId", out var id))
            ConversationId = id?.ToString() ?? string.Empty;

        if (query.TryGetValue("conversationName", out var name))
            ConversationName = Uri.UnescapeDataString(name?.ToString() ?? "聊天");

        if (query.TryGetValue("conversationType", out var type) && int.TryParse(type?.ToString(), out var t))
            ConversationType = t;

        if (query.TryGetValue("pendingContactId", out var contactId))
            PendingContactId = Uri.UnescapeDataString(contactId?.ToString() ?? string.Empty);

        if (query.TryGetValue("pendingContactType", out var contactType))
            PendingContactType = Uri.UnescapeDataString(contactType?.ToString() ?? string.Empty);
    }

    public async Task OnAppearingAsync()
    {
        // 防止并发：如果上一次 OnAppearingAsync 还没跑完，先让它失效
        if (_isAppearing)
        {
            _loadVersion++; // 让旧操作的所有 await 后检查失败
        }
        _isAppearing = true;

        ResetState();
        var version = _loadVersion;

        if (_authService == null)
        {
            ErrorMessage = "服务未初始化";
            _isAppearing = false;
            return;
        }

        try
        {
            _currentUserId = await ResolveCurrentUserIdAsync();
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] OnAppearing: userId={_currentUserId}, PendingContactId={PendingContactId}, PendingContactType={PendingContactType}, ConversationId={ConversationId}");
            if (version != _loadVersion) { _isAppearing = false; return; }

            // 从通讯录导航：会话尚未创建，先创建再加载
            if (!string.IsNullOrEmpty(PendingContactId) && !string.IsNullOrEmpty(PendingContactType))
            {
                IsLoading = true;
                var conversationId = await CreatePendingConversationAsync();
                if (version != _loadVersion) { _isAppearing = false; return; }
                if (conversationId == null)
                {
                    IsLoading = false;
                    _isAppearing = false;
                    return;
                }
                _currentConversationId = conversationId.Value;
                ConversationId = conversationId.Value.ToString();
                System.Diagnostics.Debug.WriteLine($"[ChatDetail] 新建会话: convId={_currentConversationId}");
            }
            else if (!string.IsNullOrEmpty(ConversationId))
            {
                _currentConversationId = Guid.Parse(ConversationId);
                System.Diagnostics.Debug.WriteLine($"[ChatDetail] 已有会话: convId={_currentConversationId}");
            }
            else
            {
                ErrorMessage = "会话ID无效";
                _isAppearing = false;
                return;
            }

            // 1. 从本地 SQLite 加载历史消息，页面秒开
            await LoadCachedMessagesAsync(version);
            if (version != _loadVersion) { _isAppearing = false; return; }

            // 2. 从内存缓存加载 SignalR 已收到的新消息（避免闪一下）
            LoadInMemoryUnreadMessages();

            // 3. 后台同步：增量补漏 + 标记已读 + 连接 SignalR
            _ = SyncFromServerAsync();
        }
        catch (Exception ex)
        {
            if (version == _loadVersion)
                ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            if (version == _loadVersion)
                _isAppearing = false;
        }
    }

    public async Task OnDisappearingAsync()
    {
        UnsubscribeFromEvents();
    }

    private void ResetState()
    {
        _loadVersion++;
        Messages.Clear();
        _displayedMessageIds.Clear();
        _lastMessageTime = DateTime.MinValue;
        ErrorMessage = null;
        IsLoading = false;
    }

    private async Task<Guid?> CreatePendingConversationAsync()
    {
        if (_chatService == null || string.IsNullOrEmpty(PendingContactId))
        {
            ErrorMessage = "服务未初始化或联系人ID无效";
            return null;
        }

        try
        {
            if (PendingContactType == "群组")
            {
                var groupId = Guid.Parse(PendingContactId);
                var groupResponse = await _chatService.GetOrCreateGroupConversationAsync(groupId);
                if (!groupResponse.Success || groupResponse.Data == null)
                {
                    ErrorMessage = $"创建群聊会话失败: {groupResponse.Error}";
                    return null;
                }
                _eventBusService?.Publish(new NewConversationEvent(groupResponse.Data));
                return groupResponse.Data.Id;
            }
            else if (PendingContactType == "设备")
            {
                var deviceId = Guid.Parse(PendingContactId);
                System.Diagnostics.Debug.WriteLine($"[ChatDetail] 创建设备会话: PendingContactId={PendingContactId}, deviceId={deviceId}");
                var deviceResponse = await _chatService.GetOrCreateDeviceConversationAsync(deviceId);
                System.Diagnostics.Debug.WriteLine($"[ChatDetail] 设备会话响应: Success={deviceResponse.Success}, ConvId={deviceResponse.Data?.Id}, Error={deviceResponse.Error}");
                if (!deviceResponse.Success || deviceResponse.Data == null)
                {
                    ErrorMessage = $"创建设备会话失败: {deviceResponse.Error}";
                    return null;
                }
                _eventBusService?.Publish(new NewConversationEvent(deviceResponse.Data));
                return deviceResponse.Data.Id;
            }
            else
            {
                var targetUserId = Guid.Parse(PendingContactId);
                var directResponse = await _chatService.GetOrCreateDirectConversationAsync(targetUserId);
                if (!directResponse.Success || directResponse.Data == null)
                {
                    ErrorMessage = $"创建会话失败: {directResponse.Error}";
                    return null;
                }
                _eventBusService?.Publish(new NewConversationEvent(directResponse.Data));
                return directResponse.Data.Id;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"创建会话失败: {ex.Message}";
            return null;
        }
    }

    private async Task<Guid> ResolveCurrentUserIdAsync()
    {
        if (_cachedUserId.HasValue) return _cachedUserId.Value;

        if (_tokenManager == null) return Guid.Empty;

        var username = await _tokenManager.GetUsernameAsync();
        if (string.IsNullOrEmpty(username)) return Guid.Empty;

        var token = await _tokenManager.GetTokenByUsernameAsync(username);
        if (token == null) return Guid.Empty;

        // 从 JWT access token 的 sub claim 解析真实用户 ID
        try
        {
            var accessToken = token.AccessToken;
            var payload = accessToken.Split('.')[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            if (doc.RootElement.TryGetProperty("sub", out var subElement))
            {
                var sub = subElement.GetString();
                if (Guid.TryParse(sub, out var userId))
                {
                    _cachedUserId = userId;
                    return userId;
                }
            }
        }
        catch
        {
            // JWT 解析失败，回退到 MD5 哈希
        }

        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(username));
        var fallbackId = new Guid(hash);
        _cachedUserId = fallbackId;
        return fallbackId;
    }

    private static string PadBase64(string base64)
    {
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }

    private const int MaxMessagesPerLoad = 20;

    /// <summary>
    /// 修正消息的 IsMine（客户端根据 SenderId 自行判断，不依赖服务端）
    /// 并将 SentAt 从 UTC 转为本地时区
    /// </summary>
    private ChatMessage FixMessage(ChatMessage msg)
    {
        msg.IsMine = msg.SenderId == _currentUserId;
        // JSON 反序列化后 Kind 通常为 Unspecified，需先指定为 UTC 再转本地
        msg.SentAt = msg.SentAt.Kind == DateTimeKind.Local
            ? msg.SentAt
            : DateTime.SpecifyKind(msg.SentAt, DateTimeKind.Utc).ToLocalTime();
        return msg;
    }

    /// <summary>
    /// 从本地缓存加载消息，立即渲染
    /// </summary>
    private async Task LoadCachedMessagesAsync(int version)
    {
        if (_messageCacheService == null) return;

        var cached = await _messageCacheService.GetCachedMessagesAsync(_currentConversationId, MaxMessagesPerLoad);
        if (version != _loadVersion) return;
        System.Diagnostics.Debug.WriteLine($"[ChatDetail] 缓存消息: convId={_currentConversationId}, count={cached?.Count ?? 0}");
        if (cached == null || !cached.Any()) return;

        foreach (var msg in cached)
        {
            if (_displayedMessageIds.Add(msg.Id))
                Messages.Add(FixMessage(msg));
        }
        _lastMessageTime = Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue;
    }

    /// <summary>
    /// 从内存缓存取出 SignalR 已收到的新消息，立即渲染，避免"闪一下"
    /// </summary>
    private void LoadInMemoryUnreadMessages()
    {
        if (_unreadMessageCacheService == null) return;
        if (_currentConversationId == Guid.Empty) return;

        var cached = _unreadMessageCacheService.GetAndClearCache(_currentConversationId);
        if (cached == null || !cached.Any()) return;

        foreach (var msg in cached)
        {
            if (_displayedMessageIds.Add(msg.Id))
            {
                var fixedMsg = FixMessage(msg);
                Messages.Add(fixedMsg);
            }
        }
        _lastMessageTime = Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue;
    }

    /// <summary>
    /// 异步：按需网络加载 + 标记已读 + 加入会话组
    /// </summary>
    private async Task SyncFromServerAsync()
    {
        var version = _loadVersion;
        var conversationId = _currentConversationId;
        try
        {
            // 首次无任何缓存时显示转圈；有缓存时后台静默同步
            if (Messages.Count == 0)
                IsLoading = true;

            // 增量补漏：从服务端拉取可能缺少的最新消息
            await LoadNetworkMessagesAsync(version, conversationId);
            if (version != _loadVersion) return;

            // 标记已读 + 通知会话列表
            await MarkAsReadAndNotifyAsync(conversationId);

            // SignalR 已由 ChatViewModel 全局连接，此处只加入会话组 + 订阅事件
            if (version != _loadVersion) return;
            SubscribeToEvents();
            IsConnected = _signalRChatService?.IsConnected ?? false;
            await JoinConversationGroupAsync(conversationId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 同步失败: {ex.Message}");
        }
        finally
        {
            if (version == _loadVersion)
                IsLoading = false;
        }
    }

    /// <summary>
    /// REST 网络加载：增量插入新消息，避免 Clear+re-add 导致 UI 闪烁
    /// </summary>
    private async Task LoadNetworkMessagesAsync(int version, Guid conversationId)
    {
        if (_chatService == null) return;

        var response = await _chatService.GetMessagesAsync(conversationId, 0, MaxMessagesPerLoad);
        if (version != _loadVersion) return;
        System.Diagnostics.Debug.WriteLine($"[ChatDetail] 网络加载: convId={conversationId}, Success={response.Success}, count={response.Data?.Count ?? 0}");
        if (!response.Success || response.Data == null)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 网络加载失败: Error={response.Error}");
            return;
        }

        var newMessages = response.Data
            .Where(m => _displayedMessageIds.Add(m.Id))
            .Select(FixMessage)
            .OrderBy(m => m.SentAt)
            .ToList();

        if (newMessages.Count == 0) return;

        // 增量插入：找到每条新消息的正确位置，避免 Clear+re-add
        foreach (var msg in newMessages)
        {
            var insertIndex = 0;
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                if (Messages[i].SentAt <= msg.SentAt)
                {
                    insertIndex = i + 1;
                    break;
                }
            }
            Messages.Insert(insertIndex, msg);
        }

        _lastMessageTime = Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue;

        // 更新本地缓存
        if (_messageCacheService != null && Messages.Any())
        {
            await _messageCacheService.CacheMessagesAsync(conversationId, Messages.ToList(), MaxMessagesPerLoad);
        }
    }

    private async Task MarkAsReadAndNotifyAsync(Guid conversationId)
    {
        if (_chatService != null)
            await _chatService.MarkAsReadAsync(conversationId);

        _eventBusService?.Publish(new ConversationReadEvent(conversationId));
    }

    private void SubscribeToEvents()
    {
        _eventBusService?.Subscribe<NewMessageEvent>(OnNewMessageEvent);
        _eventBusService?.Subscribe<SignalRReconnectedEvent>(OnSignalRReconnectedEvent);
    }

    private void UnsubscribeFromEvents()
    {
        _eventBusService?.Unsubscribe<NewMessageEvent>(OnNewMessageEvent);
        _eventBusService?.Unsubscribe<SignalRReconnectedEvent>(OnSignalRReconnectedEvent);
    }

    private async void OnNewMessageEvent(NewMessageEvent @event)
    {
        await HandleMessageForCurrentConversation(@event.Message);
    }

    private async void OnSignalRReconnectedEvent(SignalRReconnectedEvent @event)
    {
        await SyncMissedMessagesAsync();
    }

    /// <summary>
    /// 加入 SignalR 会话组，确保能收到该会话的实时消息
    /// </summary>
    private async Task JoinConversationGroupAsync(Guid conversationId)
    {
        if (_signalRChatService == null || conversationId == Guid.Empty) return;

        try
        {
            await _signalRChatService.JoinConversationAsync(conversationId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 加入会话组失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 处理当前会话的实时消息：跳过自己发送的回声，追加显示 + 标记已读 + 通知列表清零
    /// </summary>
    private async Task HandleMessageForCurrentConversation(ChatMessage message)
    {
        if (message.ConversationId != _currentConversationId) return;
        if (message.SenderId == _currentUserId) return;

        var conversationId = _currentConversationId;
        var version = _loadVersion;

        if (_displayedMessageIds.Add(message.Id))
        {
            _lastMessageTime = message.SentAt;
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (version == _loadVersion)
                    Messages.Add(FixMessage(message));
            });

            if (_messageCacheService != null)
            {
                await _messageCacheService.AppendMessageAsync(conversationId, message);
            }

            await MarkAsReadAndNotifyAsync(conversationId);
        }
    }

    [RelayCommand]
    private async Task RefreshMessagesAsync()
    {
        if (_chatService == null || _currentConversationId == Guid.Empty) return;

        var version = ++_loadVersion;
        var conversationId = _currentConversationId;
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Messages.Clear();
            _displayedMessageIds.Clear();
            _lastMessageTime = DateTime.MinValue;

            var response = await _chatService.GetMessagesAsync(conversationId, 0, MaxMessagesPerLoad);
            if (version != _loadVersion) return;
            if (response.Success && response.Data != null)
            {
                foreach (var msg in response.Data)
                {
                    if (_displayedMessageIds.Add(msg.Id))
                        Messages.Add(FixMessage(msg));
                }
                _lastMessageTime = Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue;

                if (_messageCacheService != null && Messages.Any())
                {
                    await _messageCacheService.CacheMessagesAsync(conversationId, Messages.ToList(), MaxMessagesPerLoad);
                }
            }

            await MarkAsReadAndNotifyAsync(conversationId);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"刷新失败: {ex.Message}";
        }
        finally
        {
            if (version == _loadVersion)
                IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (_chatService == null || string.IsNullOrWhiteSpace(MessageInput)) return;
        if (_currentConversationId == Guid.Empty) return;

        var content = MessageInput;
        MessageInput = string.Empty;

        try
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 发送消息: convId={_currentConversationId}, content={content}");
            var response = await _chatService.SendMessageAsync(_currentConversationId, content);
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 发送响应: Success={response.Success}, msgId={response.Data?.Id}, Error={response.Error}");

            if (response.Success && response.Data != null)
            {
                var message = response.Data;
                if (_displayedMessageIds.Add(message.Id))
                {
                    _lastMessageTime = message.SentAt;
                    Messages.Add(FixMessage(message));
                }

                if (_messageCacheService != null)
                {
                    await _messageCacheService.AppendMessageAsync(_currentConversationId, message);
                }

                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = $"发送失败: {response.Error ?? "未知错误"}";
                System.Diagnostics.Debug.WriteLine($"[ChatDetail] 发送失败: Error={response.Error}");
                MessageInput = content;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 发送失败: {ex.Message}");
            ErrorMessage = $"发送失败: {ex.Message}";
            MessageInput = content;
        }
    }

    private async Task SyncMissedMessagesAsync()
    {
        if (_chatService == null || _currentConversationId == Guid.Empty) return;

        var version = _loadVersion;
        try
        {
            var response = await _chatService.GetMessagesAsync(_currentConversationId);
            if (version != _loadVersion) return;
            if (!response.Success || response.Data == null) return;

            var missed = response.Data.Where(m => m.SentAt > _lastMessageTime).ToList();
            if (!missed.Any()) return;

            foreach (var msg in missed)
            {
                if (_displayedMessageIds.Add(msg.Id))
                {
                    var fixedMsg = FixMessage(msg);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Messages.Add(fixedMsg);
                    });
                }
            }
            _lastMessageTime = Messages.LastOrDefault()?.SentAt ?? DateTime.MinValue;

            // 补漏的消息也保存到本地缓存
            if (_messageCacheService != null && Messages.Any())
            {
                await _messageCacheService.CacheMessagesAsync(_currentConversationId, Messages.ToList(), MaxMessagesPerLoad);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatDetail] 补漏失败: {ex.Message}");
        }
    }
}
