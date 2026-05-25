using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AlphaAgent.Maui.ViewModels;

public partial class DiscoveryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "发现";

    [ObservableProperty]
    private string _statusMessage = "加载中...";

    public DiscoveryViewModel()
    {
        LoadData();
    }

    private async void LoadData()
    {
        try
        {
            StatusMessage = "发现页面加载完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GoToMomentsAsync()
    {
        await Shell.Current.GoToAsync("MomentsPage");
    }

    [RelayCommand]
    private async Task GoToVideoChannelsAsync()
    {
        await Shell.Current.GoToAsync("VideoChannelsPage");
    }
}