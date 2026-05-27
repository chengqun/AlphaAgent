using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Domain.Enums;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Services;

namespace AlphaAgent.Maui.ViewModels;

public partial class ContactDetailViewModel : ObservableObject, IQueryAttributable
{
    private readonly IRelationshipService? _relationshipService;
    private readonly IEventBusService? _eventBusService;

    [ObservableProperty]
    private string _contactId = string.Empty;

    [ObservableProperty]
    private string _relationshipId = string.Empty;

    [ObservableProperty]
    private string _contactName = "联系人详情";

    [ObservableProperty]
    private string _contactInitial = "?";

    [ObservableProperty]
    private string _contactType = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public ContactDetailViewModel(IRelationshipService? relationshipService = null, IEventBusService? eventBusService = null)
    {
        _relationshipService = relationshipService;
        _eventBusService = eventBusService;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            ContactId = id?.ToString() ?? string.Empty;

        if (query.TryGetValue("relationshipId", out var relationshipId))
            RelationshipId = relationshipId?.ToString() ?? string.Empty;

        if (query.TryGetValue("name", out var name))
            ContactName = name?.ToString() ?? "联系人详情";

        if (query.TryGetValue("initial", out var initial))
            ContactInitial = initial?.ToString() ?? "?";

        if (query.TryGetValue("type", out var type))
            ContactType = type?.ToString() ?? string.Empty;
    }

    [RelayCommand]
    private async Task ViewMomentsAsync()
    {
        if (string.IsNullOrEmpty(ContactId))
        {
            StatusMessage = "无法查看该用户的朋友圈";
            return;
        }

        string type = ContactType switch
        {
            "好友" => "friendship",
            "股票" => "stock",
            "设备" => "device",
            "群组" => "group",
            _ => "friendship"
        };

        await Shell.Current.GoToAsync(
            $"ContactMomentsPage?" +
            $"targetId={Uri.EscapeDataString(ContactId)}&" +
            $"targetType={Uri.EscapeDataString(type)}&" +
            $"username={Uri.EscapeDataString(ContactName)}");
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrEmpty(ContactId))
        {
            StatusMessage = "无法发送消息";
            return;
        }

        try
        {
            // 股票类型走 Agent 通道
            if (ContactType == "股票")
            {
                await Shell.Current.GoToAsync(
                    $"AgentChatDetailPage?" +
                    $"stockId={Uri.EscapeDataString(ContactId)}&" +
                    $"stockName={Uri.EscapeDataString(ContactName)}");
                return;
            }

            // 其他类型走普通聊天
            var conversationType = ContactType == "群组" ? 1 : 0;
            await Shell.Current.GoToAsync(
                $"ChatDetailPage?" +
                $"pendingContactId={Uri.EscapeDataString(ContactId)}&" +
                $"pendingContactType={Uri.EscapeDataString(ContactType)}&" +
                $"conversationName={Uri.EscapeDataString(ContactName)}&" +
                $"conversationType={conversationType}");
        }
        catch (Exception ex)
        {
            StatusMessage = $"导航失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteRelationshipAsync()
    {
        if (string.IsNullOrEmpty(RelationshipId))
        {
            StatusMessage = "无法删除关系";
            System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 错误: RelationshipId 为空");
            return;
        }

        if (_relationshipService == null)
        {
            StatusMessage = "服务未初始化";
            System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 错误: _relationshipService 为 null");
            return;
        }

        var relationshipType = ContactType switch
        {
            "好友" => (int)RelationshipType.Friendship,
            "股票" => (int)RelationshipType.Stock,
            "设备" => (int)RelationshipType.Device,
            "群组" => (int)RelationshipType.Group,
            _ => (int)RelationshipType.Friendship
        };

        StatusMessage = $"正在删除与 {ContactName} 的关系...";
        System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 开始删除 - RelationshipId: {RelationshipId}, ContactId: {ContactId}, ContactName: {ContactName}, ContactType: {ContactType}, RelationshipType: {relationshipType}");

        try
        {
            System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 调用 API - relationshipType: {relationshipType}, relationshipId: {RelationshipId}");
            
            var result = await _relationshipService.RemoveRelationshipAsync(relationshipType, RelationshipId);

            System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] API 返回 - Success: {result.Success}, Error: {result.Error}");

            if (result.Success)
            {
                System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 删除成功，准备返回上一页");
                _eventBusService?.Publish(new ContactChangedEvent("deleted"));
                await Shell.Current.Navigation.PopAsync();
            }
            else
            {
                StatusMessage = result.Error ?? "删除失败";
                System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 删除失败 - Error: {result.Error}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"删除失败: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[DeleteRelationship] 异常 - Message: {ex.Message}, StackTrace: {ex.StackTrace}");
        }
    }
}