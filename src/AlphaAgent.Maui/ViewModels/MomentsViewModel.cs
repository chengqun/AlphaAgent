using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Moment;
using AlphaAgent.Application.Dtos.Moment;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace AlphaAgent.Maui.ViewModels;

public partial class MomentsViewModel : ObservableObject
{
    private readonly IMomentService? _momentService;
    private readonly IMomentCacheService? _momentCacheService;
    private readonly HashSet<string> _displayedMomentIds = new();

    [ObservableProperty]
    private string _title = "朋友圈";

    [ObservableProperty]
    private string _statusMessage = "加载中...";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _newMomentContent = string.Empty;

    [ObservableProperty]
    private string? _newMomentImageUrl;

    public ObservableCollection<MomentItem> Moments { get; } = new ObservableCollection<MomentItem>();

    public MomentsViewModel(IMomentService? momentService = null, IMomentCacheService? momentCacheService = null)
    {
        _momentService = momentService;
        _momentCacheService = momentCacheService;
    }

    public void LoadMoments()
    {
        LoadMomentsAsync();
    }

    private async void LoadMomentsAsync()
    {
        try
        {
            // 1. 从缓存加载（瞬间完成）
            await LoadCachedMomentsAsync();

            // 缓存加载完就关闭转圈，用户立即可见
            StatusMessage = Moments.Count == 0 ? "暂无动态" : string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }

        // 2. 后台增量同步（不阻塞 UI）
        _ = SyncInBackgroundAsync();
    }

    private async Task SyncInBackgroundAsync()
    {
        try
        {
            await SyncIncrementalAsync();

            if (Moments.Count == 0)
                StatusMessage = "暂无动态";
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[MomentsViewModel] 后台同步失败: {ex.Message}");
        }
    }

    private async Task LoadCachedMomentsAsync()
    {
        if (_momentCacheService == null) return;

        var cached = await _momentCacheService.GetCachedMomentsAsync();
        foreach (var dto in cached)
        {
            if (_displayedMomentIds.Add(dto.Id.ToString()))
            {
                Moments.Add(ToMomentItem(dto));
            }
        }
    }

    private async Task SyncIncrementalAsync()
    {
        if (_momentService == null) return;

        DateTime? since = null;
        if (_momentCacheService != null)
        {
            since = await _momentCacheService.GetLatestCachedCreatedAtAsync();
        }

        var response = await _momentService.GetFriendsMomentsAsync(50, 0, since);
        if (response.Success && response.Data != null && response.Data.Count > 0)
        {
            var newItems = new List<MomentDto>();
            foreach (var dto in response.Data)
            {
                if (_displayedMomentIds.Add(dto.Id.ToString()))
                {
                    newItems.Add(dto);
                }
            }

            // 插入到列表前面（新数据比缓存更新）
            for (int i = newItems.Count - 1; i >= 0; i--)
            {
                Moments.Insert(0, ToMomentItem(newItems[i]));
            }

            // 写入缓存
            if (_momentCacheService != null)
            {
                await _momentCacheService.UpdateCacheAsync(response.Data);
            }
        }
    }

    private static DateTime EnsureLocalTime(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Local
            ? dt
            : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
    }

    private static MomentItem ToMomentItem(MomentDto dto) => new()
    {
        Id = dto.Id.ToString(),
        Username = dto.Username,
        Avatar = dto.Username.Length > 0 ? dto.Username[0].ToString().ToUpper() : "?",
        Content = dto.Content,
        ImageUrl = dto.ImageUrl,
        Time = FormatTime(dto.CreatedAt),
        Likes = 0,
        Comments = 0
    };

    private static string FormatTime(DateTime createdAt)
    {
        var localTime = EnsureLocalTime(createdAt);
        var now = DateTime.Now;
        var diff = now - localTime;

        if (diff.TotalMinutes < 1)
            return "刚刚";
        if (diff.TotalMinutes < 60)
            return $"{(int)diff.TotalMinutes}分钟前";
        if (diff.TotalHours < 24)
            return $"{(int)diff.TotalHours}小时前";
        if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}天前";
        return localTime.ToString("yyyy-MM-dd");
    }

    [RelayCommand]
    private async Task RefreshMomentsAsync()
    {
        IsLoading = true;
        try
        {
            await SyncIncrementalAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddMomentAsync()
    {
        try
        {
            Debug.WriteLine("[MomentsViewModel] AddMomentAsync 开始执行");

            if (Shell.Current == null)
            {
                Debug.WriteLine("[MomentsViewModel] Shell.Current 为 null");
                StatusMessage = "无法显示对话框";
                return;
            }

            var content = await Shell.Current.DisplayPromptAsync(
                "发布朋友圈",
                "说点什么吧...",
                "发布",
                "取消",
                placeholder: "分享新鲜事...",
                maxLength: 500);

            if (string.IsNullOrWhiteSpace(content))
                return;

            StatusMessage = "发布中...";
            IsLoading = true;

            if (_momentService == null)
            {
                StatusMessage = "服务未初始化";
                return;
            }

            var response = await _momentService.CreateMomentAsync(new CreateMomentDto
            {
                Content = content,
                Type = "Text",
                Visibility = "Friends"
            });

            if (response.Success && response.Data != null)
            {
                StatusMessage = "发布成功";

                // 追加到列表 + 写入缓存
                if (_displayedMomentIds.Add(response.Data.Id.ToString()))
                {
                    Moments.Insert(0, ToMomentItem(response.Data));
                }

                if (_momentCacheService != null)
                {
                    await _momentCacheService.AddMomentAsync(response.Data);
                }
            }
            else
            {
                StatusMessage = $"发布失败: {response.Error}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"发布失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public class MomentItem : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Avatar { get; set; } = "?";
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string Time { get; set; } = string.Empty;

    public bool HasImage => !string.IsNullOrEmpty(ImageUrl);

    private int _likes;
    public int Likes
    {
        get => _likes;
        set => SetProperty(ref _likes, value);
    }

    public int Comments { get; set; }
}
