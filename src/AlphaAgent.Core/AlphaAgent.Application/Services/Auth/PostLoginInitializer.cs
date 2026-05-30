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
    private readonly ILlmConfigService? _llmConfigService;
    private readonly IAgentConfigService? _agentConfigService;
    private readonly ISecurityClientSyncService? _securityClientSyncService;

    public PostLoginInitializer(
        ITokenManager tokenManager,
        AgentOptions agentOptions,
        ISignalRChatService? signalRChatService = null,
        ILlmConfigService? llmConfigService = null,
        IAgentConfigService? agentConfigService = null,
        ISecurityClientSyncService? securityClientSyncService = null)
    {
        _tokenManager = tokenManager;
        _agentOptions = agentOptions;
        _signalRChatService = signalRChatService;
        _llmConfigService = llmConfigService;
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
        result.AgentConfigLoaded = await LoadAndApplyConfigAsync();
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

    private async Task<bool> LoadAndApplyConfigAsync()
    {
        var userId = await ResolveCurrentUserIdAsync();
        if (userId == null) return false;

        // 1. 同步 LLM 配置 → 设置 DefaultLlm + AgentLlmConfigs
        await LoadAndApplyLlmConfigAsync(userId.Value);

        // 2. 同步 Agent 配置 → 设置 AgentSystemPrompts + EnabledTools + 更新 AgentLlmConfigs
        await LoadAndApplyAgentConfigAsync(userId.Value);

        return true;
    }

    private async Task LoadAndApplyLlmConfigAsync(Guid userId)
    {
        if (_llmConfigService == null) return;

        try
        {
            // 先用缓存
            var cachedConfigs = await _llmConfigService.GetCachedConfigsAsync(userId);
            if (cachedConfigs != null && cachedConfigs.Count > 0)
                ApplyLlmConfigs(cachedConfigs);

            // 再从服务器同步
            var syncedConfigs = await _llmConfigService.SyncFromServerAsync(userId);
            if (syncedConfigs != null && syncedConfigs.Count > 0)
            {
                ApplyLlmConfigs(syncedConfigs);
                _cachedLlmConfigs = syncedConfigs;  // 缓存供 ApplyAgentConfigs 使用
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostLoginInitializer] 加载LLM配置失败: {ex.Message}");
        }
    }

    private async Task LoadAndApplyAgentConfigAsync(Guid userId)
    {
        if (_agentConfigService == null) return;

        try
        {
            // 先用缓存
            var cachedConfigs = await _agentConfigService.GetCachedConfigsAsync(userId);
            if (cachedConfigs != null && cachedConfigs.Count > 0)
                ApplyAgentConfigs(cachedConfigs);

            // 再从服务器同步
            var syncedConfigs = await _agentConfigService.SyncFromServerAsync(userId);
            var currentConfigs = syncedConfigs ?? cachedConfigs ?? new List<AgentConfigResponseDto>();

            // 不再调用 EnsureDefaultConfigsAsync（LLM 配置已独立，无需为 Workflow 创建空占位配置）

            var finalConfigs = await _agentConfigService.GetCachedConfigsAsync(userId);
            if (finalConfigs != null && finalConfigs.Count > 0)
                ApplyAgentConfigs(finalConfigs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PostLoginInitializer] 加载Agent配置失败: {ex.Message}");
        }
    }

    private void ApplyLlmConfigs(List<LlmConfigResponseDto> configs)
    {
        // 设置默认 LLM 配置
        var defaultConfig = configs.FirstOrDefault(c => c.IsDefault) ?? configs.FirstOrDefault();
        if (defaultConfig != null && !string.IsNullOrWhiteSpace(defaultConfig.ApiKey))
        {
            _agentOptions.DefaultLlm = new LlmOptions
            {
                Name = defaultConfig.Name,
                ModelName = defaultConfig.ModelName,
                ApiKey = defaultConfig.ApiKey,
                Endpoint = defaultConfig.Endpoint,
                Temperature = defaultConfig.Temperature
            };
        }
    }

    private List<LlmConfigResponseDto>? _cachedLlmConfigs;

    private void ApplyAgentConfigs(List<AgentConfigResponseDto> configs)
    {
        _agentOptions.AgentSystemPrompts.Clear();
        _agentOptions.EnabledTools.Clear();
        _agentOptions.AgentLlmConfigs.Clear();

        foreach (var config in configs.Where(c => c.IsActive))
        {
            if (string.IsNullOrWhiteSpace(config.AgentName)) continue;

            if (!string.IsNullOrWhiteSpace(config.DefaultSystemPrompt))
                _agentOptions.AgentSystemPrompts[config.AgentName] = config.DefaultSystemPrompt;

            if (config.EnabledTools != null && config.EnabledTools.Count > 0)
                _agentOptions.EnabledTools[config.AgentName] = config.EnabledTools;

            // 如果 Agent 指定了 LlmConfigId，查找对应的 LlmOptions
            if (config.LlmConfigId.HasValue && config.LlmConfigId.Value != Guid.Empty
                && _cachedLlmConfigs != null)
            {
                var llmDto = _cachedLlmConfigs.FirstOrDefault(l => l.Id == config.LlmConfigId.Value);
                if (llmDto != null && !string.IsNullOrWhiteSpace(llmDto.ApiKey))
                {
                    _agentOptions.AgentLlmConfigs[config.AgentName] = new LlmOptions
                    {
                        Name = llmDto.Name,
                        ModelName = llmDto.ModelName,
                        ApiKey = llmDto.ApiKey,
                        Endpoint = llmDto.Endpoint,
                        Temperature = llmDto.Temperature
                    };
                }
            }
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
        }
        return false;
    }

    private static string PadBase64(string base64)
    {
        var padding = base64.Length % 4;
        if (padding == 0) return base64;
        return base64 + new string('=', 4 - padding);
    }
}