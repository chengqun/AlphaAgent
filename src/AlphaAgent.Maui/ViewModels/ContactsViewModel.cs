using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AlphaAgent.Maui.ViewModels;

public partial class ContactsViewModel : ObservableObject
{
    private readonly IRelationshipService? _relationshipService;
    private readonly IAgentService? _agentService;
    private readonly IEventBusService? _eventBusService;
    private readonly IContactSyncService? _contactSyncService;
    private bool _isLoaded = false;
    private bool _isSubscribed = false;
    private bool _forceRefresh = false;
    private DateTime _lastSyncTime = DateTime.MinValue;
    private static readonly TimeSpan _minSyncInterval = TimeSpan.FromSeconds(30);

    // 记录上次渲染的联系人 ID 集合，用于增量更新时判断数据是否变化
    private HashSet<string> _lastRenderedIds = new();

    [ObservableProperty]
    private string _title = "通讯录";

    [ObservableProperty]
    private string _statusMessage = "加载中...";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ContactItem? _selectedContact;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<ContactGroup> ContactGroups { get; } = new ObservableCollection<ContactGroup>();

    public ContactsViewModel(IRelationshipService? relationshipService = null, IAgentService? agentService = null, IEventBusService? eventBusService = null, IContactSyncService? contactSyncService = null)
    {
        _relationshipService = relationshipService;
        _agentService = agentService;
        _eventBusService = eventBusService;
        _contactSyncService = contactSyncService;
    }

    public async Task LoadContactsAsync()
    {
        SubscribeEvents();

        if (!_isLoaded)
        {
            await LoadFromCacheAsync();
            _ = SyncInBackgroundAsync();
            _isLoaded = true;
        }
        else if (_forceRefresh || ShouldBackgroundSync())
        {
            _forceRefresh = false;
            _ = SyncInBackgroundAsync();
        }
    }

    private async Task LoadFromCacheAsync()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            var cachedData = _contactSyncService != null
                ? await _contactSyncService.GetCachedContactsAsync(userId)
                : null;

            if (cachedData != null)
            {
                RenderContactBook(cachedData);
                return;
            }

            // 无缓存时走服务端（首次使用）
            IsLoading = true;
            await LoadContactsFromApiAsync();
            IsLoading = false;
            _isLoaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactsViewModel] 加载缓存失败: {ex.Message}");
        }
    }

    private async Task SyncInBackgroundAsync()
    {
        if (_contactSyncService == null) return;

        try
        {
            var userId = await GetCurrentUserIdAsync();
            var serverData = await _contactSyncService.SyncFromServerAsync(userId);

            if (serverData != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // 增量更新：只在数据变化时才刷新 UI
                    UpdateContactBookIfNeeded(serverData);
                    _lastSyncTime = DateTime.UtcNow;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactsViewModel] 后台同步失败: {ex.Message}");
        }
    }

    private bool ShouldBackgroundSync()
    {
        return DateTime.UtcNow - _lastSyncTime > _minSyncInterval;
    }

    private async Task<Guid> GetCurrentUserIdAsync()
    {
        return new Guid("11111111-1111-1111-1111-111111111111");
    }

    /// <summary>
    /// 判断数据是否变化，只在变化时才重建 UI（避免闪烁）
    /// </summary>
    private void UpdateContactBookIfNeeded(ContactBookDto contacts)
    {
        var newIds = new HashSet<string>();
        foreach (var f in contacts.Friends) newIds.Add($"friend_{f.TargetId}");
        foreach (var g in contacts.Groups) newIds.Add($"group_{g.TargetId}");
        foreach (var d in contacts.Devices) newIds.Add($"device_{d.TargetId}");
        foreach (var s in contacts.Stocks) newIds.Add($"stock_{s.TargetId}");
        foreach (var sa in contacts.ServiceAccounts) newIds.Add($"serviceaccount_{sa.TargetId}");

        if (newIds.SetEquals(_lastRenderedIds))
            return; // 数据没变，不刷新

        RenderContactBook(contacts);
    }

    /// <summary>
    /// 全量重建通讯录 UI（仅在数据变化或首次加载时调用）
    /// </summary>
    private void RenderContactBook(ContactBookDto contacts)
    {
        // 收集新数据的 ID 集合
        var newIds = new HashSet<string>();
        foreach (var f in contacts.Friends) newIds.Add($"friend_{f.TargetId}");
        foreach (var g in contacts.Groups) newIds.Add($"group_{g.TargetId}");
        foreach (var d in contacts.Devices) newIds.Add($"device_{d.TargetId}");
        foreach (var s in contacts.Stocks) newIds.Add($"stock_{s.TargetId}");
        foreach (var sa in contacts.ServiceAccounts) newIds.Add($"serviceaccount_{sa.TargetId}");
        _lastRenderedIds = newIds;

        ContactGroups.Clear();

        // 添加"新的朋友"特殊分组
        var newFriendItem = new ContactItem
        {
            Id = "newfriends",
            Name = "新的朋友",
            Type = "",
            ActionType = "NewFriends"
        };
        newFriendItem.SetCustomIconSource("addfriend");
        ContactGroups.Add(new ContactGroup
        {
            Title = "",
            Contacts = new ObservableCollection<ContactItem> { newFriendItem }
        });

        var acceptedFriends = contacts.Friends.ToList();
        if (acceptedFriends.Count > 0)
        {
            ContactGroups.Add(new ContactGroup
            {
                Title = $"好友 ({acceptedFriends.Count})",
                Contacts = new ObservableCollection<ContactItem>(
                    acceptedFriends.Select(f => new ContactItem
                    {
                        Id = f.TargetId,
                        RelationshipId = f.Id.ToString(),
                        Name = f.TargetName,
                        Initial = f.Initial,
                        Type = "好友"
                    }))
            });
        }

        var acceptedGroups = contacts.Groups.ToList();
        if (acceptedGroups.Count > 0)
        {
            ContactGroups.Add(new ContactGroup
            {
                Title = $"群组 ({acceptedGroups.Count})",
                Contacts = new ObservableCollection<ContactItem>(
                    acceptedGroups.Select(g => new ContactItem
                    {
                        Id = g.TargetId,
                        RelationshipId = g.Id.ToString(),
                        Name = g.TargetName,
                        Initial = g.Initial,
                        Type = "群组"
                    }))
            });
        }

        var acceptedDevices = contacts.Devices.ToList();
        if (acceptedDevices.Count > 0)
        {
            ContactGroups.Add(new ContactGroup
            {
                Title = $"设备 ({acceptedDevices.Count})",
                Contacts = new ObservableCollection<ContactItem>(
                    acceptedDevices.Select(d => new ContactItem
                    {
                        Id = d.TargetId,
                        RelationshipId = d.Id.ToString(),
                        Name = d.TargetName,
                        Initial = d.Initial,
                        Type = "设备",
                        DeviceType = d.DeviceType
                    }))
            });
        }

        var acceptedStocks = contacts.Stocks.ToList();
        if (acceptedStocks.Count > 0)
        {
            ContactGroups.Add(new ContactGroup
            {
                Title = $"股票 ({acceptedStocks.Count})",
                Contacts = new ObservableCollection<ContactItem>(
                    acceptedStocks.Select(s => new ContactItem
                    {
                        Id = s.TargetId,
                        RelationshipId = s.Id.ToString(),
                        Name = s.TargetName,
                        Initial = s.Initial,
                        Type = "股票"
                    }))
            });
        }

        var acceptedServiceAccounts = contacts.ServiceAccounts.ToList();
        if (acceptedServiceAccounts.Count > 0)
        {
            ContactGroups.Add(new ContactGroup
            {
                Title = $"服务号 ({acceptedServiceAccounts.Count})",
                Contacts = new ObservableCollection<ContactItem>(
                    acceptedServiceAccounts.Select(sa => new ContactItem
                    {
                        Id = sa.TargetId,
                        RelationshipId = sa.Id.ToString(),
                        Name = sa.TargetName,
                        Initial = sa.Initial,
                        Type = "服务号"
                    }))
            });
        }

        _ = LoadAiAgentsAsync();

        if (ContactGroups.Sum(g => g.Contacts.Count) == 1)
        {
            StatusMessage = "暂无联系人";
        }
        else
        {
            StatusMessage = string.Empty;
        }
    }

    private void SubscribeEvents()
    {
        if (_isSubscribed) return;
        _isSubscribed = true;
        _eventBusService?.Subscribe<ContactChangedEvent>(OnContactChanged);
    }

    private async void OnContactChanged(ContactChangedEvent @event)
    {
        _forceRefresh = true;
        _lastSyncTime = DateTime.MinValue;
        await SyncInBackgroundAsync();
    }

    private async Task LoadContactsFromApiAsync()
    {
        if (_relationshipService == null) return;

        var response = await _relationshipService.GetAcceptedContactsAsync();
        if (response.Success && response.Data != null)
        {
            RenderContactBook(response.Data);
        }
        else
        {
            await LoadAiAgentsAsync();
            StatusMessage = "通讯录加载失败";
        }
    }

    private async Task LoadAiAgentsAsync()
    {
        if (_agentService == null) return;

        try
        {
            var agents = await _agentService.GetAvailableAgentsAsync();
            if (agents.Any())
            {
                ContactGroups.Add(new ContactGroup
                {
                    Title = $"AI 助手 ({agents.Count})",
                    Contacts = new ObservableCollection<ContactItem>(
                        agents.Select(a => new ContactItem
                        {
                            Id = a.Name,
                            Name = a.Name,
                            Initial = a.Name[0].ToString(),
                            Type = "AI助手",
                            ActionType = "AgentDetail"
                        }))
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ContactsViewModel] 加载 AI 助手失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task RefreshContactsAsync()
    {
        IsLoading = true;
        try
        {
            if (_contactSyncService != null)
            {
                var userId = await GetCurrentUserIdAsync();
                var data = await _contactSyncService.SyncFromServerAsync(userId);
                if (data != null)
                    RenderContactBook(data);
            }
            else
            {
                await LoadContactsFromApiAsync();
            }
            _lastSyncTime = DateTime.UtcNow;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectContact(ContactItem contact)
    {
        SelectedContact = contact;
    }

    [RelayCommand]
    private async Task NavigateToContactDetailAsync(ContactItem contact)
    {
        if (contact == null) return;

        if (!string.IsNullOrEmpty(contact.ActionType))
        {
            switch (contact.ActionType)
            {
                case "NewFriends":
                    await Shell.Current.GoToAsync("NewFriendsPage");
                    return;
                case "AgentDetail":
                    await Shell.Current.GoToAsync(
                        $"AgentContactDetailPage?agentName={Uri.EscapeDataString(contact.Name)}");
                    return;
            }
        }

        // 服务号走详情页
        if (contact.Type == "服务号")
        {
            await Shell.Current.GoToAsync($"ServiceAccountDetailPage?id={Uri.EscapeDataString(contact.Id)}");
            return;
        }

        await Shell.Current.GoToAsync(
            $"ContactDetailPage?" +
            $"id={Uri.EscapeDataString(contact.Id)}&" +
            $"relationshipId={Uri.EscapeDataString(contact.RelationshipId)}&" +
            $"name={Uri.EscapeDataString(contact.Name)}&" +
            $"initial={Uri.EscapeDataString(contact.Initial)}&" +
            $"type={Uri.EscapeDataString(contact.Type)}");
    }

    [RelayCommand]
    private async Task AddFriendAsync()
    {
        await Shell.Current.GoToAsync("AddFriendPage");
    }

    [RelayCommand]
    private async Task NewFriendsAsync()
    {
        await Shell.Current.GoToAsync("NewFriendsPage");
    }
}

public class ContactGroup : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public ObservableCollection<ContactItem> Contacts { get; set; } = new ObservableCollection<ContactItem>();
}

public class ContactItem : ObservableObject
{
    private string? _deviceType;
    private string? _customIconSource;

    public string Id { get; set; } = string.Empty;
    public string RelationshipId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = "?";
    public string Type { get; set; } = string.Empty;
    public string? ActionType { get; set; }

    public string? DeviceType
    {
        get => _deviceType;
        set
        {
            if (SetProperty(ref _deviceType, value))
            {
                OnPropertyChanged(nameof(IconSource));
            }
        }
    }

    public void SetCustomIconSource(string? iconSource)
    {
        _customIconSource = iconSource;
        OnPropertyChanged(nameof(IconSource));
    }

    public string? IconSource
    {
        get
        {
            if (!string.IsNullOrEmpty(_customIconSource))
                return _customIconSource;

            if (!string.IsNullOrEmpty(DeviceType))
            {
                if (DeviceType.Contains("windows", StringComparison.OrdinalIgnoreCase))
                    return "windows";
                if (DeviceType.Contains("macos", StringComparison.OrdinalIgnoreCase))
                    return "macos";
                if (DeviceType.Contains("claude-bridge", StringComparison.OrdinalIgnoreCase))
                    return "claude_bridge";
            }
            return null;
        }
    }
}
