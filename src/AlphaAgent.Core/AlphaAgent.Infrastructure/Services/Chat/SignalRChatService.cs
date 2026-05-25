using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Abstractions.Chat;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AlphaAgent.Infrastructure.Services.Chat;

public class SignalRChatService : ISignalRChatService
{
    private HubConnection? _hubConnection;
    private readonly Func<HttpMessageHandler>? _httpMessageHandlerFactory;

    /// <summary>
    /// 创建 SignalRChatService，可选传入 HttpMessageHandler 工厂用于自定义 SSL 证书验证
    /// </summary>
    public SignalRChatService(Func<HttpMessageHandler>? httpMessageHandlerFactory = null)
    {
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
    }

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    public event Func<ChatMessage, Task>? OnMessageReceived;
    public event Func<Guid, Guid, int, Task>? OnUnreadCountUpdated;
    public event Func<Task>? OnReconnected;

    public async Task ConnectAsync(string accessToken, string baseUrl)
    {
        // 复用已存在的连接：仅在 Disconnected 或 null 时才重建
        if (_hubConnection != null && _hubConnection.State != HubConnectionState.Disconnected)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] 连接已存在，状态: {_hubConnection.State}，跳过重建");
            return;
        }

        if (_hubConnection != null)
        {
            await DisconnectAsync();
        }

        var hubUrl = baseUrl.TrimEnd('/') + "/hubs/chat";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(accessToken);
                if (_httpMessageHandlerFactory != null)
                {
                    options.HttpMessageHandlerFactory = _ => _httpMessageHandlerFactory();
                }
            })
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        _hubConnection.On<ChatMessage>("ReceiveMessage", async message =>
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] ReceiveMessage: ConversationId={message.ConversationId}, SenderId={message.SenderId}, Content={message.Content?.Substring(0, Math.Min(message.Content?.Length ?? 0, 30))}...");
            if (OnMessageReceived != null)
                await OnMessageReceived.Invoke(message);
        });

        _hubConnection.On<Guid, Guid, int>("UnreadCountUpdated", async (conversationId, userId, unreadCount) =>
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] UnreadCountUpdated: ConversationId={conversationId}, UserId={userId}, UnreadCount={unreadCount}");
            if (OnUnreadCountUpdated != null)
                await OnUnreadCountUpdated.Invoke(conversationId, userId, unreadCount);
        });

        _hubConnection.Reconnected += async _ =>
        {
            System.Diagnostics.Debug.WriteLine("[SignalR] Reconnected");
            if (OnReconnected != null)
                await OnReconnected.Invoke();
        };

        System.Diagnostics.Debug.WriteLine($"[SignalR] 正在连接到 {hubUrl}");
        await _hubConnection.StartAsync();
        System.Diagnostics.Debug.WriteLine($"[SignalR] 连接成功，状态: {_hubConnection.State}");
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            await _hubConnection.StopAsync();
            await _hubConnection.DisposeAsync();
            _hubConnection = null;
        }
    }

    public async Task JoinConversationAsync(Guid conversationId)
    {
        if (_hubConnection == null || _hubConnection.State != HubConnectionState.Connected)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] JoinConversation 跳过：未连接");
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("JoinConversation", conversationId);
            System.Diagnostics.Debug.WriteLine($"[SignalR] JoinConversation 成功: {conversationId}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] JoinConversation 失败: {ex.Message}");
        }
    }
}