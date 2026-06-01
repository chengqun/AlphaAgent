using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Maui.ViewModels;

/// <summary>
/// AI 设置 ViewModel：管理股票模式默认 Agent 选择等配置。
/// </summary>
public partial class AiSettingsViewModel : ObservableObject
{
    private const string StockModeAgentNameKey = "AlphaAgent_StockModeAgentName";
    private const string DefaultStockModeAgentName = "指标分析Agent";

    private readonly IAgentService? _agentService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _selectedAgentIndex = -1;

    /// <summary>所有可用的 Agent/Workflow 列表</summary>
    public ObservableCollection<AgentInfoDto> AvailableAgents { get; } = new();

    /// <summary>当前选择的描述文本</summary>
    public string CurrentSelectionText
    {
        get
        {
            if (SelectedAgentIndex >= 0 && SelectedAgentIndex < AvailableAgents.Count)
            {
                var agent = AvailableAgents[SelectedAgentIndex];
                return $"当前选择：{agent.Name}\n{agent.Description}";
            }
            return "请选择一个AI助手";
        }
    }

    public AiSettingsViewModel(IAgentService? agentService = null)
    {
        _agentService = agentService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        if (_agentService == null) return;

        IsLoading = true;
        try
        {
            // 将 HTTP + SQLite I/O 放到后台线程
            var agents = await Task.Run(async () => await _agentService.GetAvailableAgentsAsync());
            AvailableAgents.Clear();
            foreach (var agent in agents)
                AvailableAgents.Add(agent);

            // 从 Preferences 读取已保存的选择并匹配到 Picker 索引
            var savedName = Preferences.Default.Get(StockModeAgentNameKey, DefaultStockModeAgentName);
            var idx = AvailableAgents.IndexOf(AvailableAgents.FirstOrDefault(a => a.Name == savedName));
            if (idx >= 0)
            {
                SelectedAgentIndex = idx;
            }
            else
            {
                // 保存的选择无效，回退到默认
                idx = AvailableAgents.IndexOf(AvailableAgents.FirstOrDefault(a => a.Name == DefaultStockModeAgentName));
                if (idx >= 0)
                {
                    SelectedAgentIndex = idx;
                    Preferences.Default.Set(StockModeAgentNameKey, DefaultStockModeAgentName);
                }
                else if (AvailableAgents.Count > 0)
                {
                    SelectedAgentIndex = 0;
                    Preferences.Default.Set(StockModeAgentNameKey, AvailableAgents[0].Name);
                }
            }

            OnPropertyChanged(nameof(CurrentSelectionText));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AiSettingsViewModel] 加载Agent列表失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedAgentIndexChanged(int value)
    {
        // 保存选择到 Preferences
        if (value >= 0 && value < AvailableAgents.Count)
        {
            var agentName = AvailableAgents[value].Name;
            Preferences.Default.Set(StockModeAgentNameKey, agentName);
        }
        OnPropertyChanged(nameof(CurrentSelectionText));
    }

    /// <summary>
    /// 获取股票模式默认 Agent 名称（供其他 ViewModel 调用）。
    /// </summary>
    public static string GetStockModeAgentName()
    {
        return Preferences.Default.Get(StockModeAgentNameKey, DefaultStockModeAgentName);
    }
}