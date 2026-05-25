using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Application.Dtos.Security;
using AlphaAgent.Domain.Enums;
using AlphaAgent.Maui.Events;
using AlphaAgent.Maui.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace AlphaAgent.Maui.ViewModels;

public partial class AddFriendViewModel : ObservableObject
{
    private readonly IRelationshipService? _relationshipService;
    private readonly IEventBusService? _eventBusService;
    private readonly ISecurityService? _securityService;
    private readonly ILogger<AddFriendViewModel>? _logger;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public ObservableCollection<SearchResultItem> SearchResults { get; } = new();

    public AddFriendViewModel(IRelationshipService? relationshipService = null, IEventBusService? eventBusService = null, ISecurityService? securityService = null, ILogger<AddFriendViewModel>? logger = null)
    {
        _relationshipService = relationshipService;
        _eventBusService = eventBusService;
        _securityService = securityService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            StatusMessage = "请输入搜索内容";
            return;
        }

        IsLoading = true;
        StatusMessage = string.Empty;
        SearchResults.Clear();

        try
        {
            if (_relationshipService == null)
            {
                StatusMessage = "服务未初始化";
                return;
            }

            // 使用新的搜索接口，只需要 keyword 参数
            var results = await _relationshipService.SearchAllTargetsAsync(SearchText);
            if (results.Success && results.Data != null)
            {
                foreach (var target in results.Data)
                {
                    var relationshipType = target.Type switch
                    {
                        "User" => RelationshipType.Friendship,
                        "Group" => RelationshipType.Group,
                        "Stock" => RelationshipType.Stock,
                        "Device" => RelationshipType.Device,
                        _ => RelationshipType.Friendship
                    };

                    var displayType = target.Type switch
                    {
                        "User" => "用户",
                        "Group" => "群组",
                        "Stock" => "股票",
                        "Device" => "设备",
                        _ => target.Type
                    };

                    // 添加日志，查看 SecurityInfo 是否正确传输
                    _logger?.LogDebug("搜索结果: Id={Id}, Name={Name}, Type={Type}, SecurityInfo={SecurityInfo}",
                        target.Id, target.Name, target.Type,
                        target.SecurityInfo != null 
                            ? $"Code={target.SecurityInfo.Code}, Type={target.SecurityInfo.SecurityType}, Exchange={target.SecurityInfo.Exchange}" 
                            : "null");

                    SearchResults.Add(new SearchResultItem
                    {
                        Id = target.Id,
                        Name = target.Name,
                        Initial = target.Name.Length > 0 ? target.Name[0].ToString().ToUpper() : "?",
                        Type = displayType,
                        RelationshipType = relationshipType,
                        SecurityInfo = target.SecurityInfo
                    });
                }
            }

            if (SearchResults.Count == 0)
            {
                StatusMessage = "未找到匹配的结果";
            }
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
    private async Task AddFriendAsync(SearchResultItem item)
    {
        if (item == null) return;

        StatusMessage = $"正在添加 {item.Name}...";
        
        try
        {
            if (_relationshipService == null)
            {
                StatusMessage = "服务未初始化";
                return;
            }

            var result = await _relationshipService.CreateRelationshipAsync((int)item.RelationshipType, item.Id);
            
            if (result.Success)
            {
                string successMessage = string.Empty;
                switch (item.RelationshipType)
                {
                    case RelationshipType.Friendship:
                        successMessage = $"已向 {item.Name} 发送好友请求";
                        break;
                    case RelationshipType.Group:
                        successMessage = $"已申请加入群组 {item.Name}";
                        break;
                    case RelationshipType.Stock:
                        successMessage = $"已添加股票 {item.Name}";
                        _logger?.LogInformation("添加股票: Name={Name}, SecurityInfo={SecurityInfo}", 
                            item.Name, 
                            item.SecurityInfo != null 
                                ? $"Code={item.SecurityInfo.Code}, Type={item.SecurityInfo.SecurityType}, Exchange={item.SecurityInfo.Exchange}" 
                                : "NULL");
                        await SyncLocalSecurityAsync(item);
                        break;
                    case RelationshipType.Device:
                        successMessage = $"已申请绑定设备 {item.Name}";
                        break;
                }
                StatusMessage = successMessage;
                SearchResults.Remove(item);
                _eventBusService?.Publish(new ContactChangedEvent("added"));
            }
            else
            {
                StatusMessage = result.Error ?? "添加失败";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"添加失败: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async Task SyncLocalSecurityAsync(SearchResultItem item)
    {
        _logger?.LogInformation("SyncLocalSecurityAsync 开始: item.SecurityInfo={SecurityInfo}", 
            item.SecurityInfo != null 
                ? $"Code={item.SecurityInfo.Code}, Type={item.SecurityInfo.SecurityType}" 
                : "NULL");

        if (_securityService == null)
        {
            _logger?.LogWarning("SyncLocalSecurityAsync 失败: _securityService 为 null");
            return;
        }

        if (item.SecurityInfo == null)
        {
            _logger?.LogWarning("SyncLocalSecurityAsync 失败: item.SecurityInfo 为 null");
            return;
        }

        try
        {
            // UpdateOrAddSecurityAsync 会检查证券是否已存在，存在则更新，不存在则新增
            _logger?.LogInformation("SyncLocalSecurityAsync 同步: Code={Code}, Name={Name}, Type={Type}, Exchange={Exchange}", 
                item.SecurityInfo.Code, item.Name, item.SecurityInfo.SecurityType, item.SecurityInfo.Exchange);
            
            await _securityService.UpdateOrAddSecurityAsync(new SecurityDto
            {
                Code = item.SecurityInfo.Code,
                Name = item.Name,
                Type = item.SecurityInfo.SecurityType,
                Exchange = item.SecurityInfo.Exchange,
                BaseCode = item.SecurityInfo.BaseCode
            });
            
            _logger?.LogInformation("SyncLocalSecurityAsync 成功");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "SyncLocalSecurityAsync 异常");
        }
    }
}

public class SearchResultItem
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Initial { get; set; } = "?";
    public string Type { get; set; } = string.Empty;
    public RelationshipType RelationshipType { get; set; }
    public TargetSecurityInfo? SecurityInfo { get; set; }
}