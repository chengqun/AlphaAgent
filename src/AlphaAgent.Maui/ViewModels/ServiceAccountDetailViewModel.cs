using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.ServiceAccount;
using AlphaAgent.Application.Dtos.ServiceAccount;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlphaAgent.Maui.ViewModels;

/// <summary>
/// 服务号详情页 ViewModel
/// </summary>
public partial class ServiceAccountDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IServiceAccountService? _serviceAccountService;

    [ObservableProperty]
    private Guid _serviceAccountId;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _avatarUrl = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private bool _isVerified;

    [ObservableProperty]
    private bool _isFollowed;

    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    [ObservableProperty]
    private int _followerCount;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasWelcomeMessage;

    public ObservableCollection<ServiceAccountPostListItemDto> Posts { get; } = new();

    public ServiceAccountDetailViewModel(IServiceAccountService? serviceAccountService = null)
    {
        _serviceAccountService = serviceAccountService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id) && Guid.TryParse(id?.ToString(), out var guid))
        {
            ServiceAccountId = guid;
        }
    }

    /// <summary>
    /// 加载服务号详情和内容列表
    /// </summary>
    public async Task LoadAsync()
    {
        if (_serviceAccountService == null || ServiceAccountId == Guid.Empty) return;

        IsLoading = true;
        try
        {
            // 加载服务号信息
            var infoResult = await _serviceAccountService.GetAsync(ServiceAccountId);
            if (infoResult.Success && infoResult.Data != null)
            {
                var info = infoResult.Data;
                Name = info.Name;
                AvatarUrl = info.AvatarUrl ?? string.Empty;
                Description = info.Description ?? string.Empty;
                Category = info.Category;
                IsVerified = info.IsVerified;
                IsFollowed = info.IsFollowedByCurrentUser;
                WelcomeMessage = info.WelcomeMessage ?? string.Empty;
                HasWelcomeMessage = !string.IsNullOrEmpty(info.WelcomeMessage);
                FollowerCount = info.FollowerCount;
            }
            else
            {
                StatusMessage = infoResult.Error ?? "加载服务号信息失败";
                return;
            }

            // 加载内容列表
            var postsResult = await _serviceAccountService.GetPostsAsync(ServiceAccountId);
            if (postsResult.Success && postsResult.Data != null)
            {
                Posts.Clear();
                foreach (var post in postsResult.Data)
                {
                    Posts.Add(post);
                }
            }

            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ServiceAccountDetailViewModel] 加载失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FollowAsync()
    {
        if (_serviceAccountService == null || ServiceAccountId == Guid.Empty) return;

        try
        {
            var result = await _serviceAccountService.FollowAsync(ServiceAccountId);
            if (result.Success)
            {
                IsFollowed = true;
                FollowerCount++;
            }
            else
            {
                StatusMessage = result.Error ?? "关注失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"关注失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UnfollowAsync()
    {
        if (_serviceAccountService == null || ServiceAccountId == Guid.Empty) return;

        try
        {
            var result = await _serviceAccountService.UnfollowAsync(ServiceAccountId);
            if (result.Success)
            {
                IsFollowed = false;
                FollowerCount = Math.Max(0, FollowerCount - 1);
            }
            else
            {
                StatusMessage = result.Error ?? "取消关注失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消关注失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadAsync();
    }
}
