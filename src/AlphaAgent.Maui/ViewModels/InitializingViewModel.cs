using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Maui.Services;

namespace AlphaAgent.Maui.ViewModels;

public partial class InitializingViewModel : ObservableObject
{
    private readonly IPostLoginInitializer? _postLoginInitializer;
    private readonly IGlobalMessageHandler? _globalMessageHandler;

    [ObservableProperty]
    private string _statusMessage = "正在初始化...";

    [ObservableProperty]
    private bool _isCompleted;

    [ObservableProperty]
    private InitStepState _signalRState = InitStepState.Pending;

    [ObservableProperty]
    private InitStepState _agentConfigState = InitStepState.Pending;

    [ObservableProperty]
    private InitStepState _securitySyncState = InitStepState.Pending;

    public event EventHandler? InitializationCompleted;

    public InitializingViewModel(IPostLoginInitializer? postLoginInitializer = null, IGlobalMessageHandler? globalMessageHandler = null)
    {
        _postLoginInitializer = postLoginInitializer;
        _globalMessageHandler = globalMessageHandler;
    }

    [RelayCommand]
    private async Task InitializeAsync()
    {
        if (_postLoginInitializer == null)
        {
            MarkCompleted();
            return;
        }

        var progress = new Progress<PostLoginProgress>(p =>
        {
            StatusMessage = p.Message;
            if (!p.Completed) return;

            switch (p.Step)
            {
                case "SignalR":
                    SignalRState = p.Success ? InitStepState.Success : InitStepState.Failed;
                    break;
                case "AgentConfig":
                    AgentConfigState = p.Success ? InitStepState.Success : InitStepState.Failed;
                    break;
                case "SecuritySync":
                    SecuritySyncState = p.Success ? InitStepState.Success : InitStepState.Failed;
                    break;
            }
        });

        await Task.Run(() => _postLoginInitializer.InitializeAsync(AppSettings.ServerBaseAddress, progress));

        _globalMessageHandler?.StartListening();

        StatusMessage = "初始化完成";
        MarkCompleted();
    }

    private void MarkCompleted()
    {
        IsCompleted = true;
        InitializationCompleted?.Invoke(this, EventArgs.Empty);
    }
}

public enum InitStepState
{
    Pending,
    InProgress,
    Success,
    Failed
}
