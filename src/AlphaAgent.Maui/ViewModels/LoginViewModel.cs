using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Services.Auth;
using AlphaAgent.Maui.Services;

namespace AlphaAgent.Maui.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IAuthService? _authService;
    private readonly ITokenManager? _tokenManager;
    private readonly ISignalRChatService? _signalRChatService;
    private readonly IGlobalMessageHandler? _globalMessageHandler;
    private readonly IAgentConfigService? _agentConfigService;
    private readonly ISecurityClientSyncService? _securityClientSyncService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoginEnabled = true;

    [ObservableProperty]
    private bool _hasError;

    public LoginViewModel()
    {
    }

    public LoginViewModel(IAuthService authService,
        ITokenManager? tokenManager = null,
        ISignalRChatService? signalRChatService = null,
        IGlobalMessageHandler? globalMessageHandler = null,
        IAgentConfigService? agentConfigService = null,
        ISecurityClientSyncService? securityClientSyncService = null)
    {
        _authService = authService;
        _tokenManager = tokenManager;
        _signalRChatService = signalRChatService;
        _globalMessageHandler = globalMessageHandler;
        _agentConfigService = agentConfigService;
        _securityClientSyncService = securityClientSyncService;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "请输入用户名和密码";
            HasError = true;
            return;
        }

        IsLoading = true;
        IsLoginEnabled = false;
        StatusMessage = "正在登录...";
        HasError = false;

        try
        {
            if (_authService != null)
            {
                var response = await _authService.LoginAsync(Username, Password);
                if (response.Success && response.Data != null)
                {
                    StatusMessage = "正在连接服务器...";
                    await TryConnectSignalRAsync();

                    StatusMessage = "正在启动消息处理器...";
                    _globalMessageHandler?.StartListening();

                    StatusMessage = "正在加载Agent配置...";
                    await LoadAgentConfigAsync();

                    StatusMessage = "正在同步股票数据...";
                    await SyncSecuritiesAsync();

                    StatusMessage = "登录成功";
                    HasError = false;
                    await Shell.Current.GoToAsync("//ChatPage");
                }
                else
                {
                    StatusMessage = response.Error ?? "用户名或密码错误";
                    HasError = true;
                }
            }
            else
            {
                await Task.Delay(1000);
                StatusMessage = "演示模式 - 登录成功";
                HasError = false;
                await Shell.Current.GoToAsync("//ChatPage");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"登录失败: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
            IsLoginEnabled = true;
        }
    }

    private async Task TryConnectSignalRAsync()
    {
        if (_signalRChatService == null || _tokenManager == null) return;

        try
        {
            var token = await _tokenManager.GetTokenByUsernameAsync(await _tokenManager.GetUsernameAsync() ?? string.Empty);
            if (token != null)
            {
                await _signalRChatService.ConnectAsync(token.AccessToken, AppSettings.ServerBaseAddress);
            }
        }
        catch (Exception)
        {
            // SignalR 连接失败不影响登录流程
        }
    }

    private async Task LoadAgentConfigAsync()
    {
        if (_agentConfigService == null || _tokenManager == null) return;

        try
        {
            var username = await _tokenManager.GetUsernameAsync();
            if (string.IsNullOrEmpty(username)) return;

            var token = await _tokenManager.GetTokenByUsernameAsync(username);
            if (token == null) return;

            var payload = token.AccessToken.Split('.')[1];
            var jsonBytes = Convert.FromBase64String(PadBase64(payload));
            using var doc = System.Text.Json.JsonDocument.Parse(jsonBytes);
            if (!doc.RootElement.TryGetProperty("sub", out var subElement)) return;
            var sub = subElement.GetString();
            if (!Guid.TryParse(sub, out var userId)) return;

            var cachedConfigs = await _agentConfigService.GetCachedConfigsAsync(userId);
            if (cachedConfigs != null && cachedConfigs.Count > 0)
            {
                ApplyAgentConfigs(cachedConfigs);
            }

            var syncedConfigs = await _agentConfigService.SyncFromServerAsync(userId);
            var currentConfigs = syncedConfigs ?? cachedConfigs ?? new List<AgentConfigResponseDto>();

            await _agentConfigService.EnsureDefaultConfigsAsync(userId, currentConfigs);

            var finalConfigs = await _agentConfigService.GetCachedConfigsAsync(userId);
            if (finalConfigs != null && finalConfigs.Count > 0)
            {
                ApplyAgentConfigs(finalConfigs);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] 加载Agent配置失败: {ex.Message}");
        }
    }

    private static void ApplyAgentConfigs(List<AgentConfigResponseDto> configs)
    {
        var options = App.Current?.Handler?.MauiContext?.Services?.GetService<AgentOptions>();
        if (options == null) return;

        var activeConfig = configs.FirstOrDefault(c => c.IsActive);
        if (activeConfig != null && !string.IsNullOrWhiteSpace(activeConfig.ApiKey))
        {
            options.ModelName = activeConfig.ModelName;
            options.ApiKey = activeConfig.ApiKey;
            options.Endpoint = activeConfig.Endpoint;
            options.Temperature = activeConfig.Temperature;
        }

        options.AgentSystemPrompts.Clear();
        foreach (var config in configs.Where(c => c.IsActive && !string.IsNullOrWhiteSpace(c.DefaultSystemPrompt)))
        {
            options.AgentSystemPrompts[config.AgentName] = config.DefaultSystemPrompt;
        }
    }

    private static string PadBase64(string base64)
    {
        var padding = base64.Length % 4;
        if (padding == 0) return base64;
        return base64 + new string('=', 4 - padding);
    }

    private async Task SyncSecuritiesAsync()
    {
        if (_securityClientSyncService == null) return;

        try
        {
            await _securityClientSyncService.SyncFromServerAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LoginViewModel] 股票数据同步失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task NavigateToRegisterAsync()
    {
        await Shell.Current.GoToAsync("RegisterPage");
    }
}
