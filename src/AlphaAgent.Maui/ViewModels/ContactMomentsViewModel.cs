using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Moment;
using AlphaAgent.Application.Dtos.Moment;
using System.Collections.ObjectModel;

namespace AlphaAgent.Maui.ViewModels;

public partial class ContactMomentsViewModel : ObservableObject, IQueryAttributable
{
    private readonly IMomentService? _momentService;
    private readonly IMomentCacheService? _momentCacheService;
    private bool _isLoaded;
    private readonly HashSet<string> _displayedMomentIds = new();

    [ObservableProperty]
    private string _title = "朋友圈";

    [ObservableProperty]
    private string _statusMessage = "加载中...";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _targetId = string.Empty;

    [ObservableProperty]
    private string _targetType = string.Empty;

    [ObservableProperty]
    private string _username = string.Empty;

    public ObservableCollection<MomentItem> Moments { get; } = new ObservableCollection<MomentItem>();

    public ContactMomentsViewModel(IMomentService? momentService = null, IMomentCacheService? momentCacheService = null)
    {
        _momentService = momentService;
        _momentCacheService = momentCacheService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        foreach (var kvp in query)
        {
            Console.WriteLine($"[ContactMomentsVM]   {kvp.Key} = {kvp.Value}");
        }

        if (query.TryGetValue("targetId", out var targetId))
            TargetId = targetId?.ToString() ?? string.Empty;

        if (query.TryGetValue("targetType", out var targetType))
            TargetType = targetType?.ToString() ?? string.Empty;

        if (query.TryGetValue("username", out var username))
            Username = username?.ToString() ?? string.Empty;

        Title = string.IsNullOrEmpty(Username) ? "朋友圈" : $"{Username}的朋友圈";
    }

    public void LoadMoments()
    {
        if (!string.IsNullOrEmpty(TargetId) && !_isLoaded)
        {
            _isLoaded = true;
            LoadMomentsAsync();
        }
    }

    public void ResetLoadState()
    {
        _isLoaded = false;
    }

    private async void LoadMomentsAsync()
    {
        IsLoading = true;
        try
        {
            // 1. 先从缓存加载
            await LoadCachedMomentsAsync();

            // 2. 再从网络增量同步
            await SyncIncrementalAsync();

            StatusMessage = Moments.Count == 0 ? "暂无动态" : "加载完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCachedMomentsAsync()
    {
        if (_momentCacheService == null) return;

        var cached = await _momentCacheService.GetCachedMomentsAsync();
        // 过滤出当前目标的动态
        var filtered = cached.Where(m => MatchesTarget(m)).ToList();
        foreach (var dto in filtered)
        {
            if (_displayedMomentIds.Add(dto.Id.ToString()))
            {
                Moments.Add(ToMomentItem(dto));
            }
        }
    }

    private bool MatchesTarget(MomentDto dto)
    {
        if (string.IsNullOrEmpty(TargetId)) return true;

        return TargetType?.ToLower() switch
        {
            "stock" => dto.Type == "Stock",
            "device" => dto.Type == "Device",
            "group" => dto.Type == "Group",
            _ => dto.UserId.ToString() == TargetId
        };
    }

    private async Task SyncIncrementalAsync()
    {
        if (_momentService == null || string.IsNullOrEmpty(TargetId)) return;

        DateTime? since = null;
        if (_momentCacheService != null)
        {
            since = await _momentCacheService.GetLatestCachedCreatedAtAsync();
        }

        var response = await _momentService.GetMomentsAsync(TargetId, TargetType, 50, 0, since);
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

            for (int i = newItems.Count - 1; i >= 0; i--)
            {
                Moments.Insert(0, ToMomentItem(newItems[i]));
            }

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
    private async Task RefreshMomentsCommandAsync()
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
}
