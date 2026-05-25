using AlphaAgent.Abp.Application.Contracts.DTOs.AgentConfig;
using AlphaAgent.Abp.Application.Contracts.Services.AgentConfig;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace AlphaAgent.Abp.Blazor.Components.Pages;

public partial class AgentConfigManagement : AbpComponentBase
{
    protected List<AgentConfigDto> agentConfigs = new();
    protected long totalCount = 0;
    protected int currentPage = 1;
    protected int pageSize = 10;
    protected int totalPages => totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
    protected string jumpPage = string.Empty;

    protected bool isLoading = false;
    protected bool isSaving = false;
    protected bool isEditing = false;
    protected bool showModal = false;

    protected AgentConfigDto editingConfig = new();
    protected Guid? editingId = null;

    protected override async Task OnInitializedAsync()
    {
        await LoadConfigsAsync();
    }

    protected async Task LoadConfigsAsync()
    {
        isLoading = true;
        try
        {
            var result = await AgentConfigAppService.GetListAsync(
                new PagedAndSortedResultRequestDto
                {
                    MaxResultCount = pageSize,
                    SkipCount = (currentPage - 1) * pageSize,
                    Sorting = "AgentName"
                }
            );
            agentConfigs = result.Items.ToList();
            totalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            await Notify.Error($"加载配置失败: {ex.Message}");
        }
        finally
        {
            isLoading = false;
        }
    }

    protected void ShowCreateModal()
    {
        isEditing = false;
        editingId = null;
        editingConfig = new AgentConfigDto();
        showModal = true;
    }

    protected void ShowEditModal(AgentConfigDto config)
    {
        isEditing = true;
        editingId = config.Id;
        editingConfig = new AgentConfigDto
        {
            Id = config.Id,
            AgentName = config.AgentName,
            ModelName = config.ModelName,
            ApiKey = config.ApiKey,
            Endpoint = config.Endpoint,
            DefaultSystemPrompt = config.DefaultSystemPrompt,
            Temperature = config.Temperature,
            IsActive = config.IsActive
        };
        showModal = true;
    }

    protected void CloseModal()
    {
        showModal = false;
    }

    protected async Task SaveConfig()
    {
        isSaving = true;
        try
        {
            if (isEditing && editingId.HasValue)
            {
                await AgentConfigAppService.UpdateAsync(editingId.Value, new AgentConfigUpdateDto
                {
                    AgentName = editingConfig.AgentName,
                    ModelName = editingConfig.ModelName,
                    ApiKey = editingConfig.ApiKey,
                    Endpoint = editingConfig.Endpoint,
                    DefaultSystemPrompt = editingConfig.DefaultSystemPrompt,
                    Temperature = editingConfig.Temperature,
                    IsActive = editingConfig.IsActive
                });
                await Notify.Success("更新成功");
            }
            else
            {
                await AgentConfigAppService.CreateAsync(new AgentConfigCreateDto
                {
                    AgentName = editingConfig.AgentName,
                    ModelName = editingConfig.ModelName,
                    ApiKey = editingConfig.ApiKey,
                    Endpoint = editingConfig.Endpoint,
                    DefaultSystemPrompt = editingConfig.DefaultSystemPrompt,
                    Temperature = editingConfig.Temperature,
                    IsActive = editingConfig.IsActive
                });
                await Notify.Success("创建成功");
            }

            CloseModal();
            await LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            await Notify.Error($"保存失败: {ex.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }

    protected async Task DeleteConfig(AgentConfigDto config)
    {
        if (!await JS.InvokeAsync<bool>("confirm", $"确定要删除配置 \"{config.AgentName}\" 吗？"))
        {
            return;
        }

        try
        {
            await AgentConfigAppService.DeleteAsync(config.Id);
            await Notify.Success("删除成功");
            await LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            await Notify.Error($"删除失败: {ex.Message}");
        }
    }

    protected async Task ActivateConfig(AgentConfigDto config)
    {
        try
        {
            await AgentConfigAppService.ActivateConfigAsync(config.Id);
            await Notify.Success("激活成功");
            await LoadConfigsAsync();
        }
        catch (Exception ex)
        {
            await Notify.Error($"激活失败: {ex.Message}");
        }
    }

    protected static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey)) return "***";
        if (apiKey.Length <= 8) return "***";
        return apiKey[..4] + "****" + apiKey[^4..];
    }

    // 分页
    protected async Task PrevPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            await LoadConfigsAsync();
        }
    }

    protected async Task NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            await LoadConfigsAsync();
        }
    }

    protected async Task GoToPage(int page)
    {
        if (page >= 1 && page <= totalPages && page != currentPage)
        {
            currentPage = page;
            await LoadConfigsAsync();
        }
    }

    protected async Task GoToPage(string pageStr)
    {
        if (int.TryParse(pageStr, out var page) && page >= 1 && page <= totalPages)
        {
            await GoToPage(page);
        }
    }

    protected IEnumerable<int> GetPageNumbers()
    {
        if (totalPages <= 7)
            return Enumerable.Range(1, totalPages);

        var pages = new List<int> { 1 };

        int start = Math.Max(2, currentPage - 2);
        int end = Math.Min(totalPages - 1, currentPage + 2);

        if (start > 2) pages.Add(-1);
        for (int i = start; i <= end; i++) pages.Add(i);
        if (end < totalPages - 1) pages.Add(-2);

        pages.Add(totalPages);
        return pages;
    }
}
