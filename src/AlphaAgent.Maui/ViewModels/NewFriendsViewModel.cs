using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Domain.Enums;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Services;
using System.Collections.ObjectModel;

namespace AlphaAgent.Maui.ViewModels;

public partial class NewFriendsViewModel : ObservableObject
{
    private readonly IRelationshipService? _relationshipService;
    private readonly IEventBusService? _eventBusService;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<PendingRequestItem> PendingRequests { get; } = new();

    public NewFriendsViewModel(IRelationshipService? relationshipService = null, IEventBusService? eventBusService = null)
    {
        _relationshipService = relationshipService;
        _eventBusService = eventBusService;
    }

    [RelayCommand]
    private async Task LoadPendingRequestsAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        PendingRequests.Clear();

        try
        {
            if (_relationshipService == null)
            {
                StatusMessage = "服务未初始化";
                return;
            }

            // 获取待处理请求（服务端已筛选）
            var contactsResult = await Task.Run(() => _relationshipService.GetPendingRequestsAsync());
            if (contactsResult.Success && contactsResult.Data != null)
            {
                var contacts = contactsResult.Data;

                foreach (var friend in contacts.Friends)
                {
                    PendingRequests.Add(new PendingRequestItem
                    {
                        Id = friend.Id.ToString(),
                        Name = friend.TargetName,
                        Initial = friend.Initial,
                        Type = "好友",
                        RelationshipType = RelationshipType.Friendship,
                        Message = "",
                        CreatedAt = FormatTime(friend.CreationTime),
                        Status = (RelationshipStatus)friend.Status
                    });
                }

                foreach (var device in contacts.Devices)
                {
                    PendingRequests.Add(new PendingRequestItem
                    {
                        Id = device.Id.ToString(),
                        Name = device.TargetName,
                        Initial = device.Initial,
                        Type = "设备",
                        RelationshipType = RelationshipType.Device,
                        Message = "",
                        CreatedAt = FormatTime(device.CreationTime),
                        Status = (RelationshipStatus)device.Status
                    });
                }

                foreach (var group in contacts.Groups)
                {
                    PendingRequests.Add(new PendingRequestItem
                    {
                        Id = group.Id.ToString(),
                        Name = group.TargetName,
                        Initial = group.Initial,
                        Type = "群组",
                        RelationshipType = RelationshipType.Group,
                        Message = "",
                        CreatedAt = FormatTime(group.CreationTime),
                        Status = (RelationshipStatus)group.Status
                    });
                }

                if (PendingRequests.Count == 0)
                {
                    StatusMessage = "暂无待处理请求";
                }
            }
            else
            {
                StatusMessage = contactsResult.Error ?? "加载请求失败";
            }
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

    [RelayCommand]
    private async Task AcceptRequestAsync(PendingRequestItem item)
    {
        if (item == null) return;

        StatusMessage = $"正在接受 {item.Name} 的{item.Type}请求...";
        System.Diagnostics.Debug.WriteLine($"[AcceptRequest] 开始接受请求 - Name: {item.Name}, Type: {item.Type}, RelationshipType: {item.RelationshipType}, Id: {item.Id}");

        try
        {
            if (_relationshipService == null)
            {
                StatusMessage = "服务未初始化";
                System.Diagnostics.Debug.WriteLine("[AcceptRequest] 错误: _relationshipService 为 null");
                return;
            }

            var relationshipType = (int)item.RelationshipType;
            System.Diagnostics.Debug.WriteLine($"[AcceptRequest] 调用 API - relationshipType: {relationshipType}, relationshipId: {item.Id}");

            var result = await Task.Run(() => _relationshipService.AcceptRelationshipAsync(relationshipType, item.Id));

            System.Diagnostics.Debug.WriteLine($"[AcceptRequest] API 返回 - Success: {result.Success}, Error: {result.Error}, Data: {result.Data}");

            if (result.Success)
            {
                StatusMessage = $"已成功接受 {item.Name} 的{item.Type}请求";
                System.Diagnostics.Debug.WriteLine($"[AcceptRequest] 成功接受请求，移除待处理项");
                PendingRequests.Remove(item);
                _eventBusService?.Publish(new ContactChangedEvent("accepted"));
            }
            else
            {
                StatusMessage = result.Error ?? "操作失败";
                System.Diagnostics.Debug.WriteLine($"[AcceptRequest] 接受请求失败 - Error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[AcceptRequest] 异常 - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task RejectRequestAsync(PendingRequestItem item)
    {
        if (item == null) return;

        StatusMessage = $"正在拒绝 {item.Name} 的{item.Type}请求...";
        System.Diagnostics.Debug.WriteLine($"[RejectRequest] 开始拒绝请求 - Name: {item.Name}, Type: {item.Type}, RelationshipType: {item.RelationshipType}, Id: {item.Id}");

        try
        {
            if (_relationshipService == null)
            {
                StatusMessage = "服务未初始化";
                System.Diagnostics.Debug.WriteLine("[RejectRequest] 错误: _relationshipService 为 null");
                return;
            }

            var relationshipType = (int)item.RelationshipType;
            System.Diagnostics.Debug.WriteLine($"[RejectRequest] 调用 API - relationshipType: {relationshipType}, relationshipId: {item.Id}");

            var result = await Task.Run(() => _relationshipService.RejectRelationshipAsync(relationshipType, item.Id));

            System.Diagnostics.Debug.WriteLine($"[RejectRequest] API 返回 - Success: {result.Success}, Error: {result.Error}, Data: {result.Data}");

            if (result.Success)
            {
                StatusMessage = $"已拒绝 {item.Name} 的{item.Type}请求";
                System.Diagnostics.Debug.WriteLine($"[RejectRequest] 成功拒绝请求，移除待处理项");
                PendingRequests.Remove(item);
            }
            else
            {
                StatusMessage = result.Error ?? "操作失败";
                System.Diagnostics.Debug.WriteLine($"[RejectRequest] 拒绝请求失败 - Error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"操作失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[RejectRequest] 异常 - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private static DateTime EnsureLocalTime(DateTime dt)
    {
        return dt.Kind == DateTimeKind.Local
            ? dt
            : DateTime.SpecifyKind(dt, DateTimeKind.Utc).ToLocalTime();
    }

    private string FormatTime(DateTime creationTime)
    {
        var localTime = EnsureLocalTime(creationTime);
        var now = DateTime.Now;
        var diff = now - localTime;

        if (diff.TotalSeconds < 60)
        {
            return "刚刚";
        }
        else if (diff.TotalMinutes < 60)
        {
            return $"{(int)diff.TotalMinutes}分钟前";
        }
        else if (diff.TotalHours < 24)
        {
            return $"{(int)diff.TotalHours}小时前";
        }
        else if (diff.TotalDays < 7)
        {
            return $"{(int)diff.TotalDays}天前";
        }
        else
        {
            return localTime.ToString("yyyy-MM-dd");
        }
    }
}

public class PendingRequestItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = "?";
    public string Type { get; set; } = string.Empty;
    public RelationshipType RelationshipType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public RelationshipStatus Status { get; set; }
}