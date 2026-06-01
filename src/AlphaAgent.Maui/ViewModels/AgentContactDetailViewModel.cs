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

    /// <summary>
    /// 是否为工作流类型（无外部工具，显示步骤信息而非工具开关）。
    /// </summary>
    [ObservableProperty]
    private bool _isWorkflow;

    /// <summary>
    /// 工作流的子 Agent 步骤列表（仅 IsWorkflow=true 时使用）。
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<WorkflowStepItem> _workflowSteps = new();

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
            // 将所有 I/O 操作（HTTP + SQLite）放到后台线程
            string? description = null;
            List<string>? enabledToolNames = null;

            await Task.Run(async () =>
            {
                if (_agentService != null)
                {
                    var agents = await _agentService.GetAvailableAgentsAsync();
                    var agent = agents.FirstOrDefault(a => a.Name == AgentName);
                    if (agent != null)
                        description = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "暂无描述";
                }

                enabledToolNames = await GetEnabledToolNamesAsync();
            });

            // 回到主线程更新 UI
            if (description != null)
                Description = description;

            var allTools = _agentFactory?.GetAllTools(AgentName) ?? Array.Empty<ToolInfo>();

            // 工作流类型：无外部工具，显示步骤信息
            if (allTools.Count == 0)
            {
                IsWorkflow = true;
                var steps = new ObservableCollection<WorkflowStepItem>();

                // 尝试从 Agent 实例获取子 Agent 信息
                try
                {
                    var agent = _agentFactory?.GetAgent(AgentName);
                    if (agent is Infrastructure.Services.AiAgent.WorkflowAgent workflowAgent
                        && workflowAgent.SubAgents.Count > 0)
                    {
                        var index = 1;
                        foreach (var sub in workflowAgent.SubAgents)
                        {
                            steps.Add(new WorkflowStepItem(
                                index++,
                                sub.DisplayName,
                                sub.Description,
                                sub.Tools.Select(t => $"{t.Name}: {t.Description}").ToList()));
                        }
                    }
                    else if (agent != null)
                    {
                        steps.Add(new WorkflowStepItem(1, agent.Name, agent.Description, new List<string>()));
                    }
                }
                catch
                {
                    // ApiKey 未配置时无法实例化，用注册信息
                    steps.Add(new WorkflowStepItem(1, AgentName, Description, new List<string>()));
                }

                WorkflowSteps = steps;
                return;
            }

            IsWorkflow = false;

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
                await Task.Run(async () =>
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
                });
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

/// <summary>
/// 工作流步骤展示项（纯显示，无开关）。
/// </summary>
public class WorkflowStepItem
{
    /// <summary>步骤序号（从 1 开始）</summary>
    public int StepIndex { get; }

    /// <summary>子 Agent 显示名称（如 "技术分析专家"）</summary>
    public string Name { get; }

    /// <summary>子 Agent 描述</summary>
    public string Description { get; }

    /// <summary>子 Agent 使用的工具列表（如 "CalculateIndicators: 计算股票的技术指标"）</summary>
    public List<string> Tools { get; }

    /// <summary>是否有工具</summary>
    public bool HasTools => Tools.Count > 0;

    public WorkflowStepItem(int stepIndex, string name, string description, List<string> tools)
    {
        StepIndex = stepIndex;
        Name = name;
        Description = description;
        Tools = tools;
    }
}