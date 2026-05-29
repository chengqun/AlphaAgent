using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Services.Auth;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlphaAgent.Maui.ViewModels;

public partial class AgentContactDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAgentService? _agentService;
    private readonly IAgentFactory? _agentFactory;
    private readonly IAgentConfigService? _agentConfigService;
    private readonly IAgentConfigCacheRepository? _configCacheRepository;
    private readonly ITokenManager? _tokenManager;
    private readonly AgentOptions _agentOptions;

    [ObservableProperty]
    private string _agentName = "未知助手";

    [ObservableProperty]
    private string _agentInitial = "?";

    [ObservableProperty]
    private string _description = "暂无描述";

    [ObservableProperty]
    private ObservableCollection<ToolToggleItem> _enabledTools = new();

    [ObservableProperty]
    private ObservableCollection<ToolToggleItem> _disabledTools = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasDisabledTools;

    private Guid? _cachedUserId;

    public AgentContactDetailViewModel(
        IAgentService? agentService = null,
        IAgentFactory? agentFactory = null,
        IAgentConfigService? agentConfigService = null,
        IAgentConfigCacheRepository? configCacheRepository = null,
        ITokenManager? tokenManager = null,
        AgentOptions? agentOptions = null)
    {
        _agentService = agentService;
        _agentFactory = agentFactory;
        _agentConfigService = agentConfigService;
        _configCacheRepository = configCacheRepository;
        _tokenManager = tokenManager;
        _agentOptions = agentOptions ?? new AgentOptions();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("agentName", out var name))
        {
            AgentName = Uri.UnescapeDataString(name?.ToString() ?? "未知助手");
            AgentInitial = AgentName.Length > 0 ? AgentName[0].ToString() : "?";
        }
    }

    [RelayCommand]
    private async Task LoadAgentInfoAsync()
    {
        try
        {
            if (_agentService != null)
            {
                var agents = await _agentService.GetAvailableAgentsAsync();
                var agent = agents.FirstOrDefault(a => a.Name == AgentName);
                if (agent != null)
                    Description = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "暂无描述";
            }

            var allTools = _agentFactory?.GetAllTools(AgentName) ?? Array.Empty<ToolInfo>();
            var enabledToolNames = await GetEnabledToolNamesAsync();

            var enabled = new ObservableCollection<ToolToggleItem>();
            var disabled = new ObservableCollection<ToolToggleItem>();

            foreach (var t in allTools)
            {
                var isEnabled = enabledToolNames == null || enabledToolNames.Contains(t.Name);
                var item = new ToolToggleItem(t.Name, t.Description, isEnabled);
                item.IsEnabledChanged += OnToolIsEnabledChanged;

                if (isEnabled)
                    enabled.Add(item);
                else
                    disabled.Add(item);
            }

            EnabledTools = enabled;
            DisabledTools = disabled;
            HasDisabledTools = disabled.Count > 0;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
    }

    private async void OnToolIsEnabledChanged(ToolToggleItem item)
    {
        if (item.IsEnabled)
        {
            if (DisabledTools.Contains(item))
            {
                DisabledTools.Remove(item);
                EnabledTools.Add(item);
            }
        }
        else
        {
            if (EnabledTools.Contains(item))
            {
                EnabledTools.Remove(item);
                DisabledTools.Add(item);
            }
        }
        HasDisabledTools = DisabledTools.Count > 0;

        // 切换即保存
        await SaveToolsConfigAsync();
    }

    private async Task SaveToolsConfigAsync()
    {
        try
        {
            var enabledNames = EnabledTools.Select(t => t.Name).ToList();
            _agentOptions.EnabledTools[AgentName] = enabledNames;

            if (_configCacheRepository != null)
            {
                var userId = await ResolveCurrentUserIdAsync();
                if (userId != null)
                {
                    var cachedConfigs = await _configCacheRepository.GetByUserIdAsync(userId.Value);
                    var existingConfig = cachedConfigs.FirstOrDefault(c => c.AgentName == AgentName);

                    if (existingConfig != null)
                    {
                        existingConfig.EnabledTools = enabledNames;
                        existingConfig.SerializeEnabledTools();
                        await _configCacheRepository.UpsertRangeAsync(new[] { existingConfig });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentContactDetailVM] 保存工具配置失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        await Shell.Current.GoToAsync(
            $"AgentChatDetailPage?agentName={Uri.EscapeDataString(AgentName)}");
    }

    private async Task<List<string>?> GetEnabledToolNamesAsync()
    {
        var fromOptions = _agentOptions.GetEnabledTools(AgentName);
        if (fromOptions != null)
            return fromOptions;

        if (_agentConfigService != null)
        {
            var userId = await ResolveCurrentUserIdAsync();
            if (userId != null)
            {
                var configs = await _agentConfigService.GetCachedConfigsAsync(userId.Value);
                var config = configs?.FirstOrDefault(c => c.AgentName == AgentName);
                if (config?.EnabledTools?.Count > 0)
                    return config.EnabledTools;
            }
        }

        return null;
    }

    private async Task<Guid?> ResolveCurrentUserIdAsync()
    {
        if (_cachedUserId != null) return _cachedUserId;
        if (_tokenManager == null) return null;

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
                {
                    _cachedUserId = userId;
                    return userId;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AgentContactDetailVM] 解析用户ID失败: {ex.Message}");
        }
        return null;
    }

    private static string PadBase64(string base64)
    {
        var padding = base64.Length % 4;
        if (padding == 0) return base64;
        return base64 + new string('=', 4 - padding);
    }
}

public partial class ToolToggleItem : ObservableObject
{
    [ObservableProperty]
    private bool _isEnabled;

    public string Name { get; }
    public string Description { get; }

    public event Action<ToolToggleItem>? IsEnabledChanged;

    public ToolToggleItem(string name, string description, bool isEnabled = true)
    {
        Name = name;
        Description = description;
        IsEnabled = isEnabled;
    }

    partial void OnIsEnabledChanged(bool value)
    {
        IsEnabledChanged?.Invoke(this);
    }
}