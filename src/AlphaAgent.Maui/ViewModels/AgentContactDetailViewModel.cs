using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Dtos.Agent;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaAgent.Maui.ViewModels;

public partial class AgentContactDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IAgentService? _agentService;

    [ObservableProperty]
    private string _agentName = "未知助手";

    [ObservableProperty]
    private string _agentInitial = "?";

    [ObservableProperty]
    private string _description = "暂无描述";

    [ObservableProperty]
    private List<ToolInfoDto> _availableTools = new();

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public AgentContactDetailViewModel(IAgentService? agentService = null)
    {
        _agentService = agentService;
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
        if (_agentService == null)
        {
            StatusMessage = "服务未初始化";
            return;
        }

        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            var agent = agents.FirstOrDefault(a => a.Name == AgentName);

            if (agent != null)
            {
                Description = !string.IsNullOrEmpty(agent.Description) ? agent.Description : "暂无描述";
                AvailableTools = agent.Tools ?? new List<ToolInfoDto>();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        await Shell.Current.GoToAsync(
            $"AgentChatDetailPage?agentName={Uri.EscapeDataString(AgentName)}");
    }

    [RelayCommand]
    private async Task ResetSessionAsync()
    {
        if (_agentService == null)
        {
            StatusMessage = "服务未初始化";
            return;
        }

        try
        {
            StatusMessage = "正在重置会话...";

            var userId = await GetCurrentUserIdAsync();
            var session = await _agentService.GetActiveSessionAsync(userId, AgentName);

            if (session != null)
            {
                await _agentService.CloseSessionAsync(session.Id);
            }

            StatusMessage = "会话已重置";
            await Task.Delay(1500);
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"重置失败: {ex.Message}";
        }
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        return new Guid("11111111-1111-1111-1111-111111111111");
    }
}
