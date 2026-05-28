using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Services.Auth;

namespace AlphaAgent.Application.Services.Auth;

public class PostLoginInitializer : IPostLoginInitializer
{
    private readonly ITokenManager _tokenManager;
    private readonly AgentOptions _agentOptions;
    private readonly ISignalRChatService? _signalRChatService;
    private readonly IAgentConfigService? _agentConfigService;
    private readonly ISecurityClientSyncService? _securityClientSyncService;

    public PostLoginInitializer(
        ITokenManager tokenManager,
        AgentOptions agentOptions,
        ISignalRChatService? signalRChatService = null,
        IAgentConfigService? agentConfigService = null,
        ISecurityClientSyncService? securityClientSyncService = null)
    {
        _tokenManager = tokenManager;
        _agentOptions = agentOptions;
        _signalRChatService = signalRChatService;
        _agentConfigService = agentConfigService;
        _securityClientSyncService = securityClientSyncService;
    }

    public async Task<PostLoginResult> InitializeAsync(string serverBaseAddress, IProgress<PostLoginProgress>? progress = null)
    {
        var result = new PostLoginResult();

        progress?.Report(new PostLoginProgress { Step = "SignalR", Message = "正在连接服务器...", Completed = false });
        result.SignalRConnected = await TryConnectSignalRAsync(serverBaseAddress);
        progress?.Report(new PostLoginProgress { Step = "SignalR", Message = "服务器连接完成", Completed = true, Success = result.SignalRConnected });

        progress?.Report(new PostLoginProgress { Step = "AgentConfig", Message = "正在加载AI配置...", Completed = false });
        result.AgentConfigLoaded = await LoadAndApplyAgentConfigAsync();
        progress?.Report(new PostLoginProgress { Step = "AgentConfig", Message = "AI配置加载完成", Completed = true, Success = result.AgentConfigLoaded });

        progress?.Report(new PostLoginProgress { Step = "SecuritySync", Message = "正在同步股票数据...", Completed = false });
        result.SecuritySynced = await SyncSecuritiesAsync();
        progress?.Report(new PostLoginProgress { Step = "SecuritySync", Message = "股票数据同步完成", Completed = true, Success = result.SecuritySynced });

        return result;
    }

    private async Task<bool> TryConnectSignalRAsync(string serverBaseAddress)
    {
        if (_signalRChatService == null) return false;

        try
        {
            await _signalRChatService.ConnectAsync(serverBaseAddress);
            return true;
        }
        catch (Exception)
        {
            // SignalR 连接失败不影响启动流程
        }
        return false;
    }

    private async Task<bool> LoadAndApplyAgentConfigAsync()
    {
        if (_agentConfigService == null) return false;

        try
        {
            var userId = await ResolveCurrentUserIdAsync();
            if (userId == null) return false;

            var cachedConfigs = await _agentConfigService.GetCachedConfigsAsync(userId.Value);
            if (cachedConfigs != null && cachedConfigs.Count > 0)
            {
                ApplyAgentConfigs(cachedConfigs);
            }

            var syncedConfigs = await _agentConfigService.SyncFromServerAsync(userId.Value);
            var currentConfigs = syncedConfigs ?? cachedConfigs ?? new List<AgentConfigResponseDto>();

            await _agentConfigService.EnsureDefaultConfigsAsync(userId.Value, currentConfigs);

            var finalConfigs = await _agentConfigService.GetCachedConfigsAsync(userId.Value);
            if (finalConfigs != null && finalConfigs.Count > 0)
            {
                ApplyAgentConfigs(finalConfigs);
            }
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostLoginInitializer] 加载Agent配置失败: {ex.Message}");
            return false;
        }
    }

    private void ApplyAgentConfigs(List<AgentConfigResponseDto> configs)
    {
        var activeConfig = configs.FirstOrDefault(c => c.IsActive);
        if (activeConfig != null && !string.IsNullOrWhiteSpace(activeConfig.ApiKey))
        {
            _agentOptions.ModelName = activeConfig.ModelName;
            _agentOptions.ApiKey = activeConfig.ApiKey;
            _agentOptions.Endpoint = activeConfig.Endpoint;
            _agentOptions.Temperature = activeConfig.Temperature;
        }

        _agentOptions.AgentSystemPrompts.Clear();
        foreach (var config in configs.Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.DefaultSystemPrompt)))
        {
            _agentOptions.AgentSystemPrompts[config.AgentName] = config.DefaultSystemPrompt;
        }
    }

    private async Task<Guid?> ResolveCurrentUserIdAsync()
    {
        try
        {
            var username = await _tokenManager.GetUsernameAsync();
            if (string.IsNullOrEmpty(username)) return null;

            var token = await _tokenManager.GetTokenByUsernameAsync(username);
            if (token == null) return null;

            var payload = token.AccessToken.Split('.')[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            using var doc = JsonDocument.Parse(jsonBytes);
            if (doc.RootElement.TryGetProperty("sub", out var subElement))
            {
                var sub = subElement.GetString();
                if (Guid.TryParse(sub, out var userId))
                    return userId;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostLoginInitializer] 解析用户ID失败: {ex.Message}");
        }
        return null;
    }

    private async Task<bool> SyncSecuritiesAsync()
    {
        if (_securityClientSyncService == null) return false;

        try
        {
            await _securityClientSyncService.SyncFromServerAsync();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostLoginInitializer] 股票数据同步失败: {ex.Message}");
            return false;
        }
    }

    private static string PadBase64(string base64)
    {
        var padding = base64.Length % 4;
        if (padding == 0) return base64;
        return base64 + new string('=', 4 - padding);
    }
}
