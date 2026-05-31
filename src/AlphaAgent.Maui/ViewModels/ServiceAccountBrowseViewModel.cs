using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.ServiceAccount;
using AlphaAgent.Application.Dtos.ServiceAccount;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlphaAgent.Maui.ViewModels;

/// <summary>
/// 服务号浏览页 ViewModel（搜索、分类筛选、推荐列表）
/// </summary>
public partial class ServiceAccountBrowseViewModel : ObservableObject
{
    private readonly IServiceAccountService? _serviceAccountService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// 搜索/筛选结果列表
    /// </summary>
    public ObservableCollection<ServiceAccountDto> ServiceAccounts { get; } = new();

    /// <summary>
    /// 分类列表
    /// </summary>
    public ObservableCollection<string> Categories { get; } = new()
    {
        "全部", "财经", "科技", "生活", "教育", "娱乐", "健康"
    };

    public ServiceAccountBrowseViewModel(IServiceAccountService? serviceAccountService = null)
    {
        _serviceAccountService = serviceAccountService;
        _selectedCategory = "全部";
    }

    /// <summary>
    /// 加载推荐服务号
    /// </summary>
    public async Task LoadRecommendedAsync()
    {
        if (_serviceAccountService == null) return;

        IsLoading = true;
        try
        {
            var result = await _serviceAccountService.GetRecommendedAsync();
            if (result.Success && result.Data != null)
            {
                ServiceAccounts.Clear();
                foreach (var sa in result.Data)
                {
                    ServiceAccounts.Add(sa);
                }
            }

            StatusMessage = ServiceAccounts.Count == 0 ? "暂无推荐服务号" : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ServiceAccountBrowseViewModel] 加载推荐失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (_serviceAccountService == null) return;
        if (string.IsNullOrWhiteSpace(SearchText)) return;

        IsLoading = true;
        try
        {
            var result = await _serviceAccountService.SearchAsync(SearchText.Trim());
            if (result.Success && result.Data != null)
            {
                ServiceAccounts.Clear();
                foreach (var sa in result.Data)
                {
                    ServiceAccounts.Add(sa);
                }
            }

            StatusMessage = ServiceAccounts.Count == 0 ? "未找到相关服务号" : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"搜索失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FilterByCategoryAsync(string category)
    {
        if (_serviceAccountService == null) return;

        SelectedCategory = category;
        IsLoading = true;
        try
        {
            if (category == "全部")
            {
                await LoadRecommendedAsync();
                return;
            }

            var result = await _serviceAccountService.GetByCategoryAsync(category);
            if (result.Success && result.Data != null)
            {
                ServiceAccounts.Clear();
                foreach (var sa in result.Data)
                {
                    ServiceAccounts.Add(sa);
                }
            }

            StatusMessage = ServiceAccounts.Count == 0 ? $"暂无{category}类服务号" : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"筛选失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task FollowAsync(ServiceAccountDto serviceAccount)
    {
        if (_serviceAccountService == null || serviceAccount == null) return;

        try
        {
            var result = await _serviceAccountService.FollowAsync(serviceAccount.Id);
            if (result.Success)
            {
                serviceAccount.IsFollowedByCurrentUser = true;
                serviceAccount.FollowerCount++;
                // 触发 UI 刷新
                var index = ServiceAccounts.IndexOf(serviceAccount);
                if (index >= 0)
                {
                    ServiceAccounts.RemoveAt(index);
                    ServiceAccounts.Insert(index, serviceAccount);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"关注失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task UnfollowAsync(ServiceAccountDto serviceAccount)
    {
        if (_serviceAccountService == null || serviceAccount == null) return;

        try
        {
            var result = await _serviceAccountService.UnfollowAsync(serviceAccount.Id);
            if (result.Success)
            {
                serviceAccount.IsFollowedByCurrentUser = false;
                serviceAccount.FollowerCount = Math.Max(0, serviceAccount.FollowerCount - 1);
                // 触发 UI 刷新
                var index = ServiceAccounts.IndexOf(serviceAccount);
                if (index >= 0)
                {
                    ServiceAccounts.RemoveAt(index);
                    ServiceAccounts.Insert(index, serviceAccount);
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"取消关注失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GoToDetailAsync(ServiceAccountDto serviceAccount)
    {
        if (serviceAccount == null) return;
        await Shell.Current.GoToAsync($"ServiceAccountDetailPage?id={Uri.EscapeDataString(serviceAccount.Id.ToString())}");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            await SearchAsync();
        }
        else if (SelectedCategory != "全部")
        {
            await FilterByCategoryAsync(SelectedCategory);
        }
        else
        {
            await LoadRecommendedAsync();
        }
    }
}
