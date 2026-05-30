using System;
using System.Text.Json;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Application.Extensions;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Infrastructure.Extensions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AlphaAgent.ConsoleDevice;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== AlphaAgent 设备终端 ===");
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var deviceConfig = configuration.GetSection("Device").Get<DeviceConfig>() ?? new DeviceConfig();
        var agentConfig = configuration.GetSection("Agent").Get<AgentOptions>() ?? new AgentOptions();
        var sqliteConn = configuration["Database:SqliteConnectionString"] ?? "Data Source=alphaagent.db";

        var baseUrl = deviceConfig.ServerUrl?.TrimEnd('/') ?? "https://localhost:44319";
        var authCode = deviceConfig.AuthorizationCode;
        var deviceName = deviceConfig.DeviceName ?? "Console设备";
        var deviceType = deviceConfig.DeviceType ?? "console";

        if (string.IsNullOrWhiteSpace(authCode))
        {
            Console.Write("请输入设备授权码: ");
            authCode = Console.ReadLine() ?? "";
        }

        if (string.IsNullOrWhiteSpace(authCode))
        {
            Console.WriteLine("授权码不能为空");
            return;
        }

        if (string.IsNullOrWhiteSpace(agentConfig.DefaultLlm.ApiKey))
        {
            Console.Write("请输入 Agent API Key: ");
            agentConfig.DefaultLlm.ApiKey = Console.ReadLine() ?? "";
        }

        // 构建 DI 容器
        var services = new ServiceCollection();
        services.AddInfrastructureServices(sqliteConn, baseUrl, agentConfig);
        services.AddApplicationServices();
        var serviceProvider = services.BuildServiceProvider();

        // 初始化数据库
        var initializer = serviceProvider.GetRequiredService<ICoreInitializer>();
        await initializer.InitializeAsync();

        // 创建 Agent 会话
        Guid sessionId;
        string agentName;
        using (var initScope = serviceProvider.CreateScope())
        {
            var agentService = initScope.ServiceProvider.GetRequiredService<IAgentService>();
            var userId = Guid.NewGuid();

            var agents = await agentService.GetAvailableAgentsAsync();
            if (agents.Count == 0)
            {
                Console.WriteLine("没有可用的 Agent，请检查配置");
                return;
            }

            agentName = agents[0].Name;
            Console.WriteLine($"可用 Agent: {agentName}");
            foreach (var t in agents[0].Tools)
                Console.WriteLine($"  工具: {t.Name} - {t.Description}");

            var session = await agentService.GetActiveSessionAsync(userId, agentName)
                ?? await agentService.StartSessionAsync(userId, agentName);
            sessionId = session.Id;
            Console.WriteLine($"Agent 会话: {session.Id}");
        }

        Console.WriteLine();

        // 连接 SignalR
        Console.WriteLine("正在连接...");
        var hubUrl = $"{baseUrl}/hubs/chat?authorizationCode={Uri.EscapeDataString(authCode)}&deviceName={Uri.EscapeDataString(deviceName)}&deviceType={Uri.EscapeDataString(deviceType)}";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        var recentSentMessageIds = new System.Collections.Concurrent.ConcurrentDictionary<Guid, byte>();

        // 被动接收消息，通过 Agent 流式处理并回复
        connection.On<JsonElement>("ReceiveMessage", async message =>
        {
            var msgId = message.TryGetProperty("id", out var id) ? id.GetGuid() : Guid.Empty;
            var content = message.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
            var convId = message.TryGetProperty("conversationId", out var ci) ? ci.GetGuid() : Guid.Empty;
            var senderName = message.TryGetProperty("senderName", out var sn) ? sn.GetString() ?? "未知" : "未知";

            if (convId == Guid.Empty) return;

            if (msgId != Guid.Empty && recentSentMessageIds.TryRemove(msgId, out _))
                return;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到来自 [{senderName}] 的指令: {content}");

            var reply = await HandleCommandStreamingAsync(serviceProvider, sessionId, content);

            try
            {
                var sentMsg = await connection.InvokeAsync<JsonElement>("SendMessage", convId, reply, "Text");
                var sentId = sentMsg.TryGetProperty("id", out var sid) ? sid.GetGuid() : Guid.Empty;
                if (sentId != Guid.Empty)
                    recentSentMessageIds.TryAdd(sentId, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"回复发送失败: {ex.Message}");
            }
        });

        connection.Reconnected += async (_) =>
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 重新连接成功");
            Console.ResetColor();
            await Task.CompletedTask;
        };

        connection.Closed += (error) =>
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 连接断开: {error?.Message ?? "未知原因"}，将自动重连...");
            Console.ResetColor();
            return Task.CompletedTask;
        };

        try
        {
            await connection.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"连接失败: {ex.Message}");
            return;
        }

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 设备已上线，等待指令...");
        Console.WriteLine("按 Enter 退出。");

        Console.ReadLine();

        using (var closeScope = serviceProvider.CreateScope())
        {
            var agentService = closeScope.ServiceProvider.GetRequiredService<IAgentService>();
            await agentService.CloseSessionAsync(sessionId);
        }

        await connection.StopAsync();
        Console.WriteLine("设备已下线。");
    }

    static async Task<string> HandleCommandStreamingAsync(IServiceProvider serviceProvider, Guid sessionId, string command)
    {
        using var scope = serviceProvider.CreateScope();
        var agentService = scope.ServiceProvider.GetRequiredService<IAgentService>();

        try
        {
            var contentBuilder = new System.Text.StringBuilder();

            await foreach (var event_ in agentService.SendMessageStreamingAsync(sessionId, command))
            {
                switch (event_)
                {
                    case AgentTextEvent textEvent:
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write(textEvent.Content);
                        contentBuilder.Append(textEvent.Content);
                        break;

                    case AgentToolCallEvent toolCallEvent:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($"[调用工具: {toolCallEvent.ToolName}]");
                        if (toolCallEvent.Input.Count > 0)
                        {
                            Console.Write($" 输入: {JsonSerializer.Serialize(toolCallEvent.Input)}");
                        }
                        Console.WriteLine();
                        break;

                    case AgentToolResultEvent toolResultEvent:
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[工具结果: {toolResultEvent.ToolName}]");
                        if (toolResultEvent.Output != null)
                        {
                            var resultPreview = JsonSerializer.Serialize(toolResultEvent.Output);
                            if (resultPreview.Length > 200)
                                resultPreview = resultPreview.Substring(0, 200) + "...";
                            Console.Write($" 输出: {resultPreview}");
                        }
                        Console.WriteLine();
                        break;
                }
            }

            Console.WriteLine();
            Console.ResetColor();

            return contentBuilder.ToString();
        }
        catch (Exception ex)
        {
            Console.ResetColor();
            var errorMsg = $"Agent 处理失败: {ex.Message}";
            Console.WriteLine($"  {errorMsg}");
            return errorMsg;
        }
    }
}

public class DeviceConfig
{
    public string? ServerUrl { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
}