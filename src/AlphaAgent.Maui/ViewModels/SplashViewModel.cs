using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Interfaces.Update;
using AlphaAgent.Application.Dtos.Update;
using AlphaAgent.Domain.Abstractions.Enums;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Maui.Services;
using AlphaAgent.Domain.Services.Auth;
using AlphaAgent.Maui.Events;
using System.Collections.Generic;

namespace AlphaAgent.Maui.ViewModels;

public partial class SplashViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly ICoreInitializer _coreInitializer;
    private readonly IThemeManager _themeManager;
    private readonly ISignalRChatService? _signalRChatService;
    private readonly ITokenManager? _tokenManager;
    private readonly IGlobalMessageHandler? _globalMessageHandler;
    private readonly IUpdateService? _updateService;
    private readonly IAgentConfigService? _agentConfigService;

    [ObservableProperty]
    private string _statusMessage = "正在初始化...";

    [ObservableProperty]
    private bool _isLoading = true;

    [ObservableProperty]
    private bool _hasUpdate;

    [ObservableProperty]
    private CheckUpdateResultDto? _updateInfo;

    public event EventHandler<SplashNavigationEventArgs>? InitializationCompleted;

    public SplashViewModel(IAuthService authService,
                          ICoreInitializer coreInitializer,
                          IThemeManager themeManager,
                          ISignalRChatService? signalRChatService = null,
                          ITokenManager? tokenManager = null,
                          IGlobalMessageHandler? globalMessageHandler = null,
                          IUpdateService? updateService = null,
                          IAgentConfigService? agentConfigService = null)
    {
        _authService = authService;
        _coreInitializer = coreInitializer;
        _themeManager = themeManager;
        _signalRChatService = signalRChatService;
        _tokenManager = tokenManager;
        _globalMessageHandler = globalMessageHandler;
        _updateService = updateService;
        _agentConfigService = agentConfigService;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        bool isLoggedIn = false;
        
        try
        {
            StatusMessage = "正在初始化...";
            await Task.Delay(300);

            StatusMessage = "正在初始化主题...";
            _themeManager.Initialize();
            await Task.Delay(300);

            StatusMessage = "正在初始化数据库...";
            await _coreInitializer.InitializeAsync();
            await Task.Delay(300);

            StatusMessage = "正在检查登录状态...";
            await Task.Delay(300);

            StatusMessage = "正在自动登录...";
            isLoggedIn = await _authService.IsLoggedInAsync();

            if (isLoggedIn)
            {
                StatusMessage = "正在连接服务器...";
                await TryConnectSignalRAsync();

                StatusMessage = "正在启动消息处理器...";
                StartGlobalMessageHandler();

                StatusMessage = "正在加载Agent配置...";
                await LoadAgentConfigAsync();
            }

            StatusMessage = "初始化完成";
            await Task.Delay(300);

            // 检查应用更新
            await CheckUpdateAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"初始化失败: {ex.Message}";
            await Task.Delay(1000);
        }
        finally
        {
            IsLoading = false;
            InitializationCompleted?.Invoke(this, new SplashNavigationEventArgs { IsLoggedIn = isLoggedIn });
        }
    }

    private async Task TryConnectSignalRAsync()
    {
        if (_signalRChatService == null || _tokenManager == null)
            return;

        try
        {
            var token = await _tokenManager.GetTokenByUsernameAsync(await _tokenManager.GetUsernameAsync() ?? string.Empty);
            if (token != null && !token.IsExpired())
            {
                await _signalRChatService.ConnectAsync(token.AccessToken, AppSettings.ServerBaseAddress);
            }
        }
        catch (Exception)
        {
            // SignalR 连接失败不影响启动流程
        }
    }

    private void StartGlobalMessageHandler()
    {
        try
        {
            _globalMessageHandler?.StartListening();
            System.Diagnostics.Debug.WriteLine("[SplashViewModel] 全局消息处理器已启动");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 启动全局消息处理器失败: {ex.Message}");
        }
    }

    private async Task LoadAgentConfigAsync()
    {
        if (_agentConfigService == null || _tokenManager == null) return;

        try
        {
            var userId = await ResolveCurrentUserIdAsync();
            if (userId == null) return;

            // 先读本地缓存，立即应用
            var cachedConfigs = await _agentConfigService.GetCachedConfigsAsync(userId.Value);
            if (cachedConfigs != null && cachedConfigs.Count > 0)
            {
                ApplyAgentConfigs(cachedConfigs);
            }

            // 后台同步服务端，更新配置
            var syncedConfigs = await _agentConfigService.SyncFromServerAsync(userId.Value);
            var currentConfigs = syncedConfigs ?? cachedConfigs ?? new List<AlphaAgent.Application.Dtos.Agent.AgentConfigResponseDto>();

            // 补全缺失的 Agent 默认配置并提交服务端
            await _agentConfigService.EnsureDefaultConfigsAsync(userId.Value, currentConfigs);

            // 重新读取最终配置并应用
            var finalConfigs = await _agentConfigService.GetCachedConfigsAsync(userId.Value);
            if (finalConfigs != null && finalConfigs.Count > 0)
            {
                ApplyAgentConfigs(finalConfigs);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 加载Agent配置失败: {ex.Message}");
        }
    }

    private static void ApplyAgentConfigs(List<AlphaAgent.Application.Dtos.Agent.AgentConfigResponseDto> configs)
    {
        var options = App.Current?.Handler?.MauiContext?.Services?.GetService<AlphaAgent.Domain.Abstractions.AiAgent.AgentOptions>();
        if (options == null) return;

        // 取第一条激活配置的模型信息作为全局默认
        var activeConfig = configs.FirstOrDefault(c => c.IsActive);
        if (activeConfig != null && !string.IsNullOrWhiteSpace(activeConfig.ApiKey))
        {
            options.ModelName = activeConfig.ModelName;
            options.ApiKey = activeConfig.ApiKey;
            options.Endpoint = activeConfig.Endpoint;
            options.Temperature = activeConfig.Temperature;
        }

        // 按 Agent 名称填充各自的 SystemPrompt
        options.AgentSystemPrompts.Clear();
        foreach (var config in configs.Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.DefaultSystemPrompt)))
        {
            options.AgentSystemPrompts[config.AgentName] = config.DefaultSystemPrompt;
        }
    }

    private async Task<Guid?> ResolveCurrentUserIdAsync()
    {
        if (_tokenManager == null) return null;

        try
        {
            var username = await _tokenManager.GetUsernameAsync();
            if (string.IsNullOrEmpty(username)) return null;

            var token = await _tokenManager.GetTokenByUsernameAsync(username);
            if (token == null) return null;

            var payload = token.AccessToken.Split('.')[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            if (doc.RootElement.TryGetProperty("sub", out var subElement))
            {
                var sub = subElement.GetString();
                if (Guid.TryParse(sub, out var userId))
                    return userId;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 解析用户ID失败: {ex.Message}");
        }

        return null;
    }

    private static string PadBase64(string base64)
    {
        var padding = base64.Length % 4;
        if (padding == 0) return base64;
        return base64 + new string('=', 4 - padding);
    }

    private async Task CheckUpdateAsync()
    {
        if (_updateService == null) return;

        try
        {
            StatusMessage = "正在检查更新...";

            var platform = GetAppPlatform();
            var versionCode = GetVersionCode();

            var result = await _updateService.CheckUpdateAsync(platform, versionCode);
            if (result.Success && result.Data?.HasUpdate == true)
            {
                HasUpdate = true;
                UpdateInfo = result.Data;
                System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 发现新版本: {result.Data.VersionName} ({result.Data.VersionCode})");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] 检查更新失败: {ex.Message}");
        }
    }

    private static AppPlatform GetAppPlatform()
    {
        var platform = DeviceInfo.Platform;
        if (platform == DevicePlatform.iOS) return AppPlatform.iOS;
        if (platform == DevicePlatform.Android) return AppPlatform.Android;
        if (platform == DevicePlatform.WinUI) return AppPlatform.Windows;
        if (platform == DevicePlatform.macOS) return AppPlatform.MacCatalyst;
        return AppPlatform.Android;
    }

    private static int GetVersionCode()
    {
        // BuildString 对应 csproj 中的 ApplicationVersion（整数版本号）
        return int.TryParse(AppInfo.Current.BuildString, out var code) ? code : 1;
    }
}