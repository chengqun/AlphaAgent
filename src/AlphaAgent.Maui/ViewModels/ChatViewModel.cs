using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Abstractions.Chat;
using AlphaAgent.Domain.Services.Auth;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Services;
using System;

namespace AlphaAgent.Maui.ViewModels;

public partial class ChatViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IChatService? _chatService;
    private readonly IAgentRepository? _agentRepository;
    private readonly IAuthService? _authService;
    private readonly ISignalRChatService? _signalRChatService;
    private readonly IEventBusService? _eventBusService;
    private readonly IUnreadMessageCacheService? _unreadMessageCacheService;
    private readonly IGlobalMessageHandler? _globalMessageHandler;
    private readonly IConversationSyncService? _conversationSyncService;
    private readonly ITokenManager? _tokenManager;
    private bool _isLoaded = false;
    private Guid? _cachedUserId;
    private bool _isSubscribed = false;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private static readonly TimeSpan _minSyncInterval = TimeSpan.FromSeconds(30);

    [ObservableProperty]
    private string _title = "聊天";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ObservableConversation? _selectedConversation;

    public ObservableCollection<ObservableConversation> Conversations { get; } = new();

    public ChatViewModel(IChatService? chatService = null,
                         IAgentRepository? agentRepository = null,
                         IAuthService? authService = null,
                         ISignalRChatService? signalRChatService = null,
                         IEventBusService? eventBusService = null,
                         IUnreadMessageCacheService? unreadMessageCacheService = null,
                         IGlobalMessageHandler? globalMessageHandler = null,
                         IConversationSyncService? conversationSyncService = null,
                         ITokenManager? tokenManager = null)
    {
        _chatService = chatService;
        _agentRepository = agentRepository;
        _authService = authService;
        _signalRChatService = signalRChatService;
        _eventBusService = eventBusService;
        _unreadMessageCacheService = unreadMessageCacheService;
        _globalMessageHandler = globalMessageHandler;
        _conversationSyncService = conversationSyncService;
        _tokenManager = tokenManager;
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        if (_cachedUserId.HasValue)
            return _cachedUserId.Value;

        // 从 JWT access token 的 sub claim 解析真实用户 ID（与 ChatDetailViewModel 一致）
        if (_tokenManager != null)
        {
            var username = await _tokenManager.GetUsernameAsync();
            if (!string.IsNullOrEmpty(username))
            {
                var token = await _tokenManager.GetTokenByUsernameAsync(username);
                if (token != null)
                {
                    try
                    {
                        var payload = token.AccessToken.Split('.')[1];
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
                    catch { }
                }
            }
        }

        // JWT 解析失败，回退到 MD5 哈希
        if (_authService != null)
        {
            var username = await _authService.GetUsernameAsync();
            if (!string.IsNullOrEmpty(username))
            {
                _cachedUserId = StringToGuid(username);
                return _cachedUserId.Value;
            }
        }

        _cachedUserId = new Guid("11111111-1111-1111-1111-111111111111");
        return _cachedUserId.Value;
    }

    private static string PadBase64(string base64)
    {
        return base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
    }

    private Guid StringToGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    public async Task OnAppearingAsync()
    {
        _globalMessageHandler?.StartListening();
        SubscribeSignalREvents();

        if (!_isLoaded)
        {
            // 首次加载：从本地缓存瞬间显示，然后后台同步
            await LoadFromCacheAsync();
            _ = SyncInBackgroundAsync();
            _isLoaded = true;
        }
        else if (ShouldBackgroundSync())
        {
            // 返回页面且距上次同步超过阈值：后台同步
            _ = SyncInBackgroundAsync();
        }

        LoadCachedUnreadMessages();
    }

    public async Task OnDisappearingAsync()
    {
        // 不取消 EventBus 订阅——保持实时事件接收
    }

    /// <summary>
    /// 从本地 SQLite 缓存加载会话列表（瞬间完成，无转圈）
    /// </summary>
    private async Task LoadFromCacheAsync()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var cachedConversations = _conversationSyncService != null
                ? await _conversationSyncService.GetCachedConversationsAsync(userId)
                : new List<Conversation>();

            Conversations.Clear();

            foreach (var conv in cachedConversations)
            {
                Conversations.Add(new ObservableConversation(conv));
            }

            await LoadAgentConversationsAsync();
            SortConversations();
            StatusMessage = Conversations.Count == 0 ? "暂无会话" : string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 加载缓存失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 后台同步服务端会话，更新本地缓存和 UI（不阻塞 UI）
    /// </summary>
    private async Task SyncInBackgroundAsync()
    {
        if (_conversationSyncService == null) return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var serverConversations = await _conversationSyncService.SyncFromServerAsync(userId);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (var serverConv in serverConversations)
                {
                    var local = Conversations.FirstOrDefault(c => c.Id == serverConv.Id);
                    if (local != null)
                    {
                        local.UpdateFromSource(serverConv);
                    }
                    else
                    {
                        Conversations.Add(new ObservableConversation(serverConv));
                    }
                }

                // 移除服务端已不存在的会话（排除 Agent 会话）
                var serverIds = serverConversations.Select(c => c.Id).ToHashSet();
                for (int i = Conversations.Count - 1; i >= 0; i--)
                {
                    if (!serverIds.Contains(Conversations[i].Id)
                        && Conversations[i].Type != 3 && Conversations[i].Type != 4)
                    {
                        Conversations.RemoveAt(i);
                    }
                }

                SortConversations();
                _lastSyncTime = DateTime.UtcNow;

                if (Conversations.Count == 0)
                    StatusMessage = "暂无会话";
                else
                    StatusMessage = string.Empty;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 后台同步失败: {ex.Message}");
        }
    }

    private bool ShouldBackgroundSync()
    {
        return DateTime.UtcNow - _lastSyncTime > _minSyncInterval;
    }

    private void LoadCachedUnreadMessages()
    {
        if (_unreadMessageCacheService == null) return;

        bool hasCachedMessages = false;
        foreach (var conv in Conversations)
        {
            if (_unreadMessageCacheService.HasCachedMessages(conv.Id))
            {
                var cached = _unreadMessageCacheService.GetAndClearCache(conv.Id);
                if (cached.Any())
                {
                    hasCachedMessages = true;
                    if (cached.Last().Content != null)
                    {
                        conv.LastMessage = cached.Last().Content;
                        conv.LastMessageTime = cached.Last().SentAt;
                    }
                }
            }
        }

        if (hasCachedMessages)
            SortConversations();
    }

    private void SubscribeSignalREvents()
    {
        if (_isSubscribed) return;
        _isSubscribed = true;

        _eventBusService?.Subscribe<NewMessageEvent>(OnNewMessage);
        _eventBusService?.Subscribe<UnreadCountUpdatedEvent>(OnUnreadCountUpdated);
        _eventBusService?.Subscribe<ConversationReadEvent>(OnConversationRead);
        _eventBusService?.Subscribe<NewConversationEvent>(OnNewConversation);
    }

    private void UnsubscribeSignalREvents()
    {
        if (!_isSubscribed) return;
        _isSubscribed = false;

        _eventBusService?.Unsubscribe<NewMessageEvent>(OnNewMessage);
        _eventBusService?.Unsubscribe<UnreadCountUpdatedEvent>(OnUnreadCountUpdated);
        _eventBusService?.Unsubscribe<ConversationReadEvent>(OnConversationRead);
        _eventBusService?.Unsubscribe<NewConversationEvent>(OnNewConversation);
    }

    private async void OnNewMessage(NewMessageEvent @event)
    {
        // 自己发送的消息不增加未读数
        var currentUserId = await GetCurrentUserIdAsync();
        var isFromSelf = @event.Message.SenderId == currentUserId;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var conv = Conversations.FirstOrDefault(c => c.Id == @event.Message.ConversationId);
            if (conv != null)
            {
                if (!isFromSelf)
                    conv.UnreadCount++;
                conv.LastMessage = @event.Message.Content;
                conv.LastMessageTime = @event.Message.SentAt;
                SortConversations();
            }
            else if (!isFromSelf)
            {
                // 收到未知会话的消息 — 创建占位条目，下次同步会用完整数据替换
                var placeholder = new Conversation
                {
                    Id = @event.Message.ConversationId,
                    Type = 0,
                    OtherUserName = @event.Message.SenderName,
                    UnreadCount = 1,
                    LastMessage = @event.Message.Content,
                    LastMessageTime = @event.Message.SentAt
                };
                Conversations.Add(new ObservableConversation(placeholder));
                SortConversations();

                _ = SavePlaceholderToCacheAsync(placeholder);
            }
        });
    }

    private async Task SavePlaceholderToCacheAsync(Conversation placeholder)
    {
        if (_conversationSyncService == null) return;
        try
        {
            var userId = await GetCurrentUserIdAsync();
            await _conversationSyncService.UpsertConversationAsync(placeholder, userId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 保存占位会话失败: {ex.Message}");
        }
    }

    private async void OnUnreadCountUpdated(UnreadCountUpdatedEvent @event)
    {
        // 只处理当前用户的未读计数更新（SignalR 广播给整个组）
        var currentUserId = await GetCurrentUserIdAsync();
        if (@event.UserId != currentUserId) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var conv = Conversations.FirstOrDefault(c => c.Id == @event.ConversationId);
            if (conv != null)
            {
                conv.UnreadCount = @event.UnreadCount;
            }
        });
    }

    private void OnConversationRead(ConversationReadEvent @event)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var conv = Conversations.FirstOrDefault(c => c.Id == @event.ConversationId);
            if (conv != null)
            {
                conv.UnreadCount = 0;
            }
        });
    }

    private async void OnNewConversation(NewConversationEvent @event)
    {
        var conv = @event.Conversation;

        // 保存到本地缓存（包括 Agent 会话，确保 APP 重启后仍可见）
        if (_conversationSyncService != null)
        {
            try
            {
                var userId = await GetCurrentUserIdAsync();
                await _conversationSyncService.UpsertConversationAsync(conv, userId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 缓存新会话失败: {ex.Message}");
            }
        }

        // 更新 UI（所有类型都添加到列表）
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var existing = Conversations.FirstOrDefault(c => c.Id == conv.Id);
            if (existing == null)
            {
                Conversations.Add(new ObservableConversation(conv));
                SortConversations();
                if (StatusMessage == "暂无会话")
                    StatusMessage = string.Empty;
            }
        });
    }

    /// <summary>
    /// 排序会话列表：只在顺序变化时才重建，减少 CollectionView 闪烁
    /// </summary>
    private void SortConversations()
    {
        var desired = Conversations.OrderByDescending(c => c.LastMessageTime).ToList();

        bool needsReorder = false;
        for (int i = 0; i < desired.Count; i++)
        {
            if (Conversations[i].Id != desired[i].Id)
            {
                needsReorder = true;
                break;
            }
        }

        if (!needsReorder) return;

        Conversations.Clear();
        foreach (var conv in desired)
        {
            Conversations.Add(conv);
        }
    }

    private async Task LoadAgentConversationsAsync()
    {
        if (_agentRepository == null) return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var agentSessions = await _agentRepository.GetUserSessionsAsync(userId);

            foreach (var session in agentSessions)
            {
                if (session.Status == AgentSessionStatus.Closed)
                    continue;

                // 跳过已在缓存列表中的会话（避免重复）
                if (Conversations.Any(c => c.Id == session.Id))
                    continue;

                var isStock = !string.IsNullOrEmpty(session.Context) && session.Context.StartsWith("stock:");
                if (isStock)
                {
                    var parts = session.Context!.Split(':', 3);
                    var stockName = parts.Length >= 3 ? parts[2] : session.AgentName;

                    Conversations.Add(new ObservableConversation(new Conversation
                    {
                        Id = session.Id,
                        Type = 4,
                        Name = stockName,
                        Context = session.Context,
                        LastMessage = session.Messages.LastOrDefault()?.Content,
                        LastMessageTime = session.LastActiveAt
                    }));
                }
                else
                {
                    Conversations.Add(new ObservableConversation(new Conversation
                    {
                        Id = session.Id,
                        Type = 3,
                        Name = session.AgentName,
                        LastMessage = session.Messages.LastOrDefault()?.Content,
                        LastMessageTime = session.LastActiveAt
                    }));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ChatViewModel] 加载 Agent 会话失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshConversationsAsync()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "刷新中...";

            if (_conversationSyncService != null)
            {
                var userId = await GetCurrentUserIdAsync();
                var allConversations = await _conversationSyncService.SyncFromServerAsync(userId);

                Conversations.Clear();
                foreach (var conv in allConversations)
                {
                    Conversations.Add(new ObservableConversation(conv));
                }
            }
            else
            {
                Conversations.Clear();
                if (_chatService != null)
                {
                    var response = await _chatService.GetMyConversationsAsync();
                    if (response.Success && response.Data != null)
                    {
                        foreach (var conv in response.Data)
                        {
                            Conversations.Add(new ObservableConversation(conv));
                        }
                    }
                }
            }

            await LoadAgentConversationsAsync();
            SortConversations();

            StatusMessage = Conversations.Count == 0 ? "暂无会话" : string.Empty;
            _isLoaded = true;
            _lastSyncTime = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            StatusMessage = $"刷新失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToChatDetailAsync()
    {
        if (SelectedConversation == null) return;

        var conv = SelectedConversation;
        SelectedConversation = null;

        if (conv.Type == 4)
        {
            var stockName = conv.Name ?? "股票";
            var stockId = conv.Id.ToString();
            if (!string.IsNullOrEmpty(conv.Context) && conv.Context.StartsWith("stock:"))
            {
                var parts = conv.Context.Split(':', 3);
                if (parts.Length >= 2) stockId = parts[1];
            }
            await Shell.Current.GoToAsync(
                $"AgentChatDetailPage?" +
                $"sessionId={conv.Id}&" +
                $"stockId={Uri.EscapeDataString(stockId)}&" +
                $"stockName={Uri.EscapeDataString(stockName)}");
            return;
        }

        if (conv.Type == 3)
        {
            await Shell.Current.GoToAsync(
                $"AgentChatDetailPage?sessionId={conv.Id}&agentName={Uri.EscapeDataString(conv.Name ?? "股票分析助手")}");
            return;
        }

        var name = conv.Type == 0
            ? Uri.EscapeDataString(conv.OtherUserName ?? "未知用户")
            : Uri.EscapeDataString(conv.Name ?? "群聊");

        await Shell.Current.GoToAsync(
            $"ChatDetailPage?conversationId={conv.Id}&conversationName={name}&conversationType={conv.Type}");
    }

    [RelayCommand]
    private async Task DeleteConversationAsync(Guid conversationId)
    {
        var conv = Conversations.FirstOrDefault(c => c.Id == conversationId);
        if (conv == null) return;

        try
        {
            IsLoading = true;

            if (conv.Type == 3 || conv.Type == 4)
            {
                if (_agentRepository != null)
                {
                    await _agentRepository.DeleteSessionAsync(conversationId);
                }
            }
            else if (_chatService != null)
            {
                await _chatService.DeleteConversationAsync(conversationId);

                // 同步删除本地缓存
                if (_conversationSyncService != null)
                {
                    await _conversationSyncService.DeleteConversationAsync(conversationId);
                }
            }

            Conversations.Remove(conv);
            StatusMessage = Conversations.Count == 0 ? "暂无会话" : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
