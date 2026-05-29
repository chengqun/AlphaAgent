using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace AlphaAgent.ClaudeBridge;

class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine("=== AlphaAgent Claude Bridge ===");
        Console.WriteLine();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var deviceConfig = configuration.GetSection("Device").Get<DeviceConfig>() ?? new DeviceConfig();
        var claudeConfig = configuration.GetSection("ClaudeCli").Get<ClaudeCliConfig>() ?? new ClaudeCliConfig();

        var baseUrl = deviceConfig.ServerUrl?.TrimEnd('/') ?? "https://localhost:44319";
        var authCode = deviceConfig.AuthorizationCode;
        var deviceName = deviceConfig.DeviceName ?? "Claude Bridge";
        var deviceType = deviceConfig.DeviceType ?? "claude-bridge";

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

        var cliService = new ClaudeCliService(claudeConfig);
        Console.WriteLine($"Claude CLI: {claudeConfig.CliPath}");
        Console.WriteLine($"工作目录: {claudeConfig.WorkingDirectory ?? Environment.CurrentDirectory}");
        if (!string.IsNullOrWhiteSpace(claudeConfig.Model))
            Console.WriteLine($"模型: {claudeConfig.Model}");
        Console.WriteLine($"预算上限: ${claudeConfig.MaxBudgetUsd}");
        Console.WriteLine();

        // 连接 SignalR
        Console.WriteLine("正在连接...");
        var hubUrl = $"{baseUrl}/hubs/chat?authorizationCode={Uri.EscapeDataString(authCode)}&deviceName={Uri.EscapeDataString(deviceName)}&deviceType={Uri.EscapeDataString(deviceType)}";

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        var recentSentMessageIds = new ConcurrentDictionary<Guid, byte>();

        // 被动接收消息，通过 Claude CLI 处理并回复
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

            var reply = await cliService.SendMessageAsync(convId, content);

            Console.ResetColor();

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

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Claude Bridge 已上线，等待指令...");
        Console.WriteLine("按 Enter 退出。");

        Console.ReadLine();

        await connection.StopAsync();
        Console.WriteLine("Claude Bridge 已下线。");
    }
}

public class DeviceConfig
{
    public string? ServerUrl { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceType { get; set; }
}

public class ClaudeCliConfig
{
    public string CliPath { get; set; } = "claude";
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public string? AllowedTools { get; set; }
    public double MaxBudgetUsd { get; set; } = 1.0;
    public int ResponseTimeoutSeconds { get; set; } = 300;
    public string? WorkingDirectory { get; set; }
}
