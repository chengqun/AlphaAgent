using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Models;
using AlphaAgent.Maui.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaAgent.Maui.ViewModels;

/// <summary>
/// Agent 聊天 ViewModel：支持普通 Agent 会话和股票 Agent 会话。
/// 股票模式通过 stockId+stockName 进入，每只股票创建独立的 Agent 会话（通过 Context 标识）。
/// </summary>
public partial class AgentChatDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAgentService? _agentService;
    private readonly IAuthService? _authService;
    private readonly IEventBusService? _eventBusService;
    private bool _isLoaded = false;
    private Guid? _currentSessionId;
    private bool _isNewSession = false;
    private string _previousKey = string.Empty;
    private int _streamVersion; // 每次 ApplyQueryAttributes 递增，用于废弃过期的流式操作

    public event Action? StreamingContentUpdated;

    [ObservableProperty]
    private string _agentName = "股票分析助手";

    [ObservableProperty]
    private string _stockId = string.Empty;

    [ObservableProperty]
    private string _stockName = string.Empty;

    /// <summary>
    /// 是否为股票模式（从通讯录股票进入），股票模式使用 Context 隔离会话并支持自动查询。
    /// </summary>
    [ObservableProperty]
    private bool _isStockMode;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private bool _canSendMessage;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public ObservableCollection<ChatMessageItem> Messages { get; } = new();

    public IAsyncRelayCommand SendMessageCommand { get; }

    public AgentChatDetailViewModel(IAgentService? agentService = null, IAuthService? authService = null, IEventBusService? eventBusService = null)
    {
        _agentService = agentService;
        _authService = authService;
        _eventBusService = eventBusService;
        SendMessageCommand = new AsyncRelayCommand(SendMessageAsync, () => CanSendMessage);
    }

    partial void OnMessageTextChanged(string value)
    {
        CanSendMessage = !string.IsNullOrWhiteSpace(value);
        SendMessageCommand.NotifyCanExecuteChanged();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        // 股票模式参数
        var hasStockId = query.TryGetValue("stockId", out var stockId);
        var hasStockName = query.TryGetValue("stockName", out var stockName);

        // 会话 ID（从聊天列表进入时传入，跳过创建）
        Guid sid = Guid.Empty;
        var hasSessionId = query.TryGetValue("sessionId", out var sessionId)
                           && Guid.TryParse(sessionId?.ToString(), out sid);

        // agentName 参数：恢复旧会话时传入会话自身的 AgentName，锁定 Agent
        var hasAgentName = query.TryGetValue("agentName", out var agentNameParam);

        // 先根据传入参数确定模式
        if (hasStockId)
        {
            IsStockMode = true;
            StockId = Uri.UnescapeDataString(stockId?.ToString() ?? string.Empty);
            StockName = Uri.UnescapeDataString(stockName?.ToString() ?? string.Empty);

            // 恢复旧会话时，用会话自身的 AgentName（锁定 Agent，不随全局设置变化）
            // 新建会话时，从全局设置读取用户选择的 Agent
            if (hasSessionId && hasAgentName)
            {
                AgentName = Uri.UnescapeDataString(agentNameParam?.ToString() ?? AiSettingsViewModel.GetStockModeAgentName());
            }
            else
            {
                AgentName = AiSettingsViewModel.GetStockModeAgentName();
            }
        }
        else
        {
            // 没有 stockId → 非股票模式，必须清除残留的股票状态
            IsStockMode = false;
            StockId = string.Empty;
            StockName = string.Empty;
        }

        // Agent 名称参数（非股票模式时生效）
        if (!IsStockMode && query.TryGetValue("agentName", out var name))
        {
            AgentName = Uri.UnescapeDataString(name?.ToString() ?? "股票分析助手");
        }

        // 构造唯一标识，检测参数变化需要重新初始化
        var currentKey = hasSessionId ? sid.ToString() : (IsStockMode ? $"stock:{StockId}" : AgentName);

        if (hasSessionId)
        {
            _currentSessionId = sid;
            _isNewSession = false;
        }
        else
        {
            // 没有 sessionId → 让 InitializeAsync 重新创建/查找
            _currentSessionId = null;
        }

        // 检测切换：不同会话需要重置状态重新初始化
        if (currentKey != _previousKey)
        {
            _previousKey = currentKey;
            _streamVersion++; // 废弃正在运行的流式操作
            _isLoaded = false;
            Messages.Clear();
        }
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (_isLoaded) return;

        if (_agentService == null)
        {
            ErrorMessage = "服务未初始化";
            return;
        }

        var version = _streamVersion;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            if (_currentSessionId == null)
            {
                await GetOrCreateSessionAsync();
            }

            if (_streamVersion != version) return; // 会话已切换，停止初始化

            await LoadMessagesAsync();

            if (_streamVersion != version) return;

            // 股票新会话：自动查询该股票
            if (_isNewSession && IsStockMode && !string.IsNullOrEmpty(StockName))
            {
                await AutoQueryStockAsync();
            }

            if (_streamVersion != version) return;

            _isLoaded = true;
        }
        catch (Exception ex)
        {
            if (_streamVersion != version) return;
            ErrorMessage = $"初始化失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[AgentChatDetail] 初始化失败: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task GetOrCreateSessionAsync()
    {
        if (_agentService == null) return;

        var userId = await GetCurrentUserIdAsync();

        if (IsStockMode)
        {
            var context = $"stock:{StockId}:{StockName}";
            var activeSession = await _agentService.GetActiveSessionByContextAsync(userId, AgentName, context);

            if (activeSession != null)
            {
                _currentSessionId = activeSession.Id;
                _isNewSession = false;
                // 复用已有会话时也通知列表，确保从通讯录进入的会话出现在聊天列表
                PublishNewAgentConversation(Type: 4, Name: $"{StockName} · {AgentName}", Context: context);
            }
            else
            {
                var session = await _agentService.StartSessionAsync(userId, AgentName, context);
                _currentSessionId = session.Id;
                _isNewSession = true;
                PublishNewAgentConversation(Type: 4, Name: $"{StockName} · {AgentName}", Context: context);
            }
        }
        else
        {
            var activeSession = await _agentService.GetActiveSessionAsync(userId, AgentName);

            if (activeSession != null)
            {
                _currentSessionId = activeSession.Id;
                _isNewSession = false;
                PublishNewAgentConversation(Type: 3, Name: AgentName, Context: null);
            }
            else
            {
                var session = await _agentService.StartSessionAsync(userId, AgentName);
                _currentSessionId = session.Id;
                PublishNewAgentConversation(Type: 3, Name: AgentName, Context: null);
            }
        }
    }

    private void PublishNewAgentConversation(int Type, string Name, string? Context)
    {
        var conversation = new Conversation
        {
            Id = _currentSessionId!.Value,
            Type = Type,
            Name = Name,
            Context = Context,
            AgentName = AgentName,
            LastMessageTime = DateTime.UtcNow
        };
        _eventBusService?.Publish(new NewConversationEvent(conversation));
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        if (_authService != null)
        {
            var username = await _authService.GetUsernameAsync();
            if (!string.IsNullOrEmpty(username))
            {
                return StringToGuid(username);
            }
        }

        return new Guid("11111111-1111-1111-1111-111111111111");
    }

    private Guid StringToGuid(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    private async Task LoadMessagesAsync()
    {
        if (_currentSessionId == null || _agentService == null) return;

        var version = _streamVersion;

        try
        {
            var messages = await _agentService.GetSessionHistoryAsync(_currentSessionId.Value, 5);
            // 加载完成前会话可能已切换，丢弃过期数据
            if (_streamVersion != version) return;

            Messages.Clear();
            foreach (var msg in messages)
            {
                foreach (var item in ExpandMessage(msg))
                {
                    Messages.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentChatDetail] 加载消息失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 股票新会话自动查询该股票，复用流式逻辑
    /// </summary>
    private async Task AutoQueryStockAsync()
    {
        if (_agentService == null || _currentSessionId == null) return;

        var query = $"分析一下{StockName}";
        MessageText = string.Empty;
        var version = _streamVersion;

        try
        {
            Messages.Add(new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = "text",
                Role = "user",
                Content = query,
                Timestamp = DateTime.Now
            });

            await foreach (var chunk in _agentService.SendMessageStreamingAsync(_currentSessionId.Value, query))
            {
                if (_streamVersion != version) return; // 会话已切换，废弃此流
                ProcessStreamEvent(chunk);
            }

            if (_streamVersion != version) return;
            FinalizeStreaming();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentChatDetail] 自动查询失败: {ex.Message}");
        }
    }

    private static DateTime EnsureLocalTime(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Local
            ? dt
            : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
    }

    private IEnumerable<ChatMessageItem> ExpandMessage(AgentChatMessageDto msg)
    {
        var role = msg.Role?.ToLowerInvariant() ?? "user";
        var timestamp = EnsureLocalTime(msg.Timestamp);

        if (role == "user")
        {
            yield return new ChatMessageItem
            {
                Id = msg.Id.ToString(),
                ItemType = "text",
                Role = "user",
                Content = msg.Content,
                Timestamp = timestamp
            };
            yield break;
        }

        if (msg.ContentParts != null && msg.ContentParts.Count > 0)
        {
            foreach (var part in msg.ContentParts.OrderBy(p => p.Index))
            {
                switch (part.Type)
                {
                    case "text":
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            yield return new ChatMessageItem
                            {
                                Id = $"{msg.Id}_text_{part.Index}",
                                ItemType = "text",
                                Role = "assistant",
                                Content = part.Text,
                                MarkdownContent = part.Text,
                                AuthorName = part.AuthorName ?? AgentName,
                                Timestamp = timestamp
                            };
                        }
                        break;
                    case "tool_call":
                        yield return new ChatMessageItem
                        {
                            Id = $"{msg.Id}_{part.ToolName}_call_{part.Index}",
                            ItemType = "tool_call",
                            Role = "assistant",
                            ToolName = part.ToolName,
                            Input = part.ToolInput ?? new Dictionary<string, object>(),
                            AuthorName = part.AuthorName ?? AgentName,
                            Timestamp = timestamp
                        };
                        break;
                    case "tool_result":
                        yield return new ChatMessageItem
                        {
                            Id = $"{msg.Id}_{part.ToolName}_result_{part.Index}",
                            ItemType = "tool_result",
                            Role = "assistant",
                            ToolName = part.ToolName,
                            Output = part.ToolOutput,
                            AuthorName = part.AuthorName ?? AgentName,
                            Timestamp = timestamp
                        };
                        break;
                }
            }
            yield break;
        }

        if (msg.ToolCalls != null)
        {
            foreach (var tc in msg.ToolCalls)
            {
                yield return new ChatMessageItem
                {
                    Id = $"{msg.Id}_{tc.ToolName}_call",
                    ItemType = "tool_call",
                    Role = "assistant",
                    ToolName = tc.ToolName,
                    Input = tc.Input,
                    Timestamp = timestamp
                };

                if (tc.Output != null)
                {
                    yield return new ChatMessageItem
                    {
                        Id = $"{msg.Id}_{tc.ToolName}_result",
                        ItemType = "tool_result",
                        Role = "assistant",
                        ToolName = tc.ToolName,
                        Output = tc.Output,
                        Timestamp = timestamp
                    };
                }
            }
        }

        if (!string.IsNullOrEmpty(msg.Content))
        {
            yield return new ChatMessageItem
            {
                Id = msg.Id.ToString(),
                ItemType = "text",
                Role = "assistant",
                Content = msg.Content,
                MarkdownContent = msg.Content,
                Timestamp = timestamp
            };
        }
    }

    private ChatMessageItem? _currentTextItem;
    private ChatMessageItem? _currentThinkingItem;

    private void ProcessStreamEvent(AgentStreamEvent event_)
    {
        switch (event_)
        {
            case AgentTextEvent textEvent:
                if (_currentThinkingItem != null)
                {
                    Messages.Remove(_currentThinkingItem);
                    _currentThinkingItem = null;
                }
                if (_currentTextItem == null)
                {
                    _currentTextItem = new ChatMessageItem
                    {
                        Id = Guid.NewGuid().ToString(),
                        ItemType = "text",
                        Role = "assistant",
                        Content = textEvent.Content,
                        IsStreaming = true,
                        AuthorName = textEvent.AuthorName ?? AgentName,
                        Timestamp = DateTime.Now
                    };
                    Messages.Add(_currentTextItem);
                }
                else
                {
                    _currentTextItem.Content += textEvent.Content;
                    // 更新 Agent 名称（子 Agent 切换时）
                    if (!string.IsNullOrEmpty(textEvent.AuthorName) && _currentTextItem.AuthorName != textEvent.AuthorName)
                        _currentTextItem.AuthorName = textEvent.AuthorName;
                }
                StreamingContentUpdated?.Invoke();
                break;

            case AgentToolCallEvent toolCallEvent:
                if (_currentTextItem != null)
                {
                    _currentTextItem.MarkdownContent = _currentTextItem.Content;
                    _currentTextItem.IsStreaming = false;
                    _currentTextItem = null;
                }
                Messages.Add(new ChatMessageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ItemType = "tool_call",
                    Role = "assistant",
                    ToolName = toolCallEvent.ToolName,
                    Input = toolCallEvent.Input,
                    AuthorName = toolCallEvent.AuthorName ?? AgentName,
                    Timestamp = DateTime.Now
                });
                _currentThinkingItem = new ChatMessageItem
                {
                    Id = $"thinking_{toolCallEvent.ToolName}",
                    ItemType = "thinking",
                    Role = "assistant",
                    AuthorName = toolCallEvent.AuthorName ?? AgentName,
                    Timestamp = DateTime.Now
                };
                Messages.Add(_currentThinkingItem);
                break;

            case AgentToolResultEvent toolResultEvent:
                if (_currentThinkingItem != null)
                {
                    Messages.Remove(_currentThinkingItem);
                    _currentThinkingItem = null;
                }
                if (_currentTextItem != null)
                {
                    _currentTextItem.MarkdownContent = _currentTextItem.Content;
                    _currentTextItem.IsStreaming = false;
                    _currentTextItem = null;
                }
                Messages.Add(new ChatMessageItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ItemType = "tool_result",
                    Role = "assistant",
                    ToolName = toolResultEvent.ToolName,
                    Output = toolResultEvent.Output,
                    AuthorName = toolResultEvent.AuthorName ?? AgentName,
                    Timestamp = DateTime.Now
                });
                break;
        }
    }

    private void FinalizeStreaming()
    {
        if (_currentThinkingItem != null)
        {
            Messages.Remove(_currentThinkingItem);
            _currentThinkingItem = null;
        }

        if (_currentTextItem != null)
        {
            _currentTextItem.MarkdownContent = _currentTextItem.Content;
            _currentTextItem.IsStreaming = false;
            _currentTextItem = null;
        }
    }

    private async Task SendMessageAsync()
    {
        if (_agentService == null || _currentSessionId == null)
        {
            ErrorMessage = "服务未初始化";
            return;
        }

        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        var content = MessageText;
        MessageText = string.Empty;
        var version = _streamVersion;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Messages.Add(new ChatMessageItem
            {
                Id = Guid.NewGuid().ToString(),
                ItemType = "text",
                Role = "user",
                Content = content,
                Timestamp = DateTime.Now
            });

            _currentTextItem = null;
            _currentThinkingItem = null;

            await foreach (var event_ in _agentService.SendMessageStreamingAsync(_currentSessionId.Value, content))
            {
                if (_streamVersion != version) return; // 会话已切换，废弃此流
                ProcessStreamEvent(event_);
            }

            if (_streamVersion != version) return;
            FinalizeStreaming();
        }
        catch (Exception ex)
        {
            if (_streamVersion != version) return; // 会话已切换，不显示过期错误
            ErrorMessage = $"发送失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[AgentChatDetail] 发送失败: {ex}");
            MessageText = content;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
