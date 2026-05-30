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
    // LLM 配置
    protected List<LlmConfigDto> llmConfigs = new();
    protected bool isLoadingLlm = false;
    protected bool showLlmModal = false;
    protected bool isEditingLlm = false;
    protected LlmConfigCreateDto editingLlm = new();

    // Agent 配置
    protected List<AgentConfigDto> agentConfigs = new();
    protected long totalCount = 0;
    protected int currentPage = 1;
    protected int pageSize = 10;
    protected int totalPages => totalCount > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1;
    protected string jumpPage = string.Empty;

    protected bool isLoadingAgent = false;
    protected bool showAgentModal = false;
    protected bool isEditingAgent = false;
    protected AgentConfigCreateDto editingAgent = new();
    protected Guid? editingAgentId = null;

    protected bool isSaving = false;

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadLlmConfigsAsync(), LoadAgentConfigsAsync());
    }

    #region LLM 配置

    protected async Task LoadLlmConfigsAsync()
    {
        isLoadingLlm = true;
        try
        {
            var result = await LlmConfigAppService.GetMyLlmConfigsAsync();
            llmConfigs = result.Items.ToList();
        }
        catch (Exception ex)
        {
            await Notify.Error($"加载LLM配置失败: {ex.Message}");
        }
        finally
        {
            isLoadingLlm = false;
        }
    }

    protected void ShowCreateLlmModal()
    {
        isEditingLlm = false;
        editingLlm = new LlmConfigCreateDto();
        showLlmModal = true;
    }

    protected void ShowEditLlmModal(LlmConfigDto config)
    {
        isEditingLlm = true;
        editingLlm = new LlmConfigCreateDto
        {
            Id = config.Id,
            Name = config.Name,
            ModelName = config.ModelName,
            ApiKey = config.ApiKey,
            Endpoint = config.Endpoint,
            Temperature = config.Temperature,
            IsDefault = config.IsDefault
        };
        showLlmModal = true;
    }

    protected void CloseLlmModal()
    {
        showLlmModal = false;
    }

    protected async Task SaveLlmConfig()
    {
        isSaving = true;
        try
        {
            await LlmConfigAppService.SetMyLlmConfigAsync(editingLlm);
            await Notify.Success(isEditingLlm ? "更新成功" : "创建成功");
            CloseLlmModal();
            await LoadLlmConfigsAsync();
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

    protected async Task SetDefaultLlm(LlmConfigDto config)
    {
        try
        {
            await LlmConfigAppService.SetDefaultLlmConfigAsync(config.Id);
            await Notify.Success("已设为默认");
            await LoadLlmConfigsAsync();
        }
        catch (Exception ex)
        {
            await Notify.Error($"设置失败: {ex.Message}");
        }
    }

    protected async Task DeleteLlmConfig(LlmConfigDto config)
    {
        if (!await JS.InvokeAsync<bool>("confirm", $"确定要删除LLM配置 \"{config.Name}\" 吗？"))
            return;

        try
        {
            await LlmConfigAppService.DeleteLlmConfigAsync(config.Id);
            await Notify.Success("删除成功");
            await Task.WhenAll(LoadLlmConfigsAsync(), LoadAgentConfigsAsync());
        }
        catch (Exception ex)
        {
            await Notify.Error($"删除失败: {ex.Message}");
        }
    }

    #endregion

    #region Agent 配置

    protected async Task LoadAgentConfigsAsync()
    {
        isLoadingAgent = true;
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
            isLoadingAgent = false;
        }
    }

    protected void ShowCreateAgentModal()
    {
        isEditingAgent = false;
        editingAgentId = null;
        editingAgent = new AgentConfigCreateDto();
        showAgentModal = true;
    }

    protected void ShowEditAgentModal(AgentConfigDto config)
    {
        isEditingAgent = true;
        editingAgentId = config.Id;
        editingAgent = new AgentConfigCreateDto
        {
            AgentName = config.AgentName,
            DefaultSystemPrompt = config.DefaultSystemPrompt,
            IsActive = config.IsActive,
            LlmConfigId = config.LlmConfigId
        };
        showAgentModal = true;
    }

    protected void CloseAgentModal()
    {
        showAgentModal = false;
    }

    protected async Task SaveAgentConfig()
    {
        isSaving = true;
        try
        {
            if (isEditingAgent && editingAgentId.HasValue)
            {
                await AgentConfigAppService.UpdateAsync(editingAgentId.Value, new AgentConfigUpdateDto
                {
                    AgentName = editingAgent.AgentName,
                    DefaultSystemPrompt = editingAgent.DefaultSystemPrompt,
                    IsActive = editingAgent.IsActive,
                    LlmConfigId = editingAgent.LlmConfigId
                });
                await Notify.Success("更新成功");
            }
            else
            {
                await AgentConfigAppService.CreateAsync(new AgentConfigCreateDto
                {
                    AgentName = editingAgent.AgentName,
                    DefaultSystemPrompt = editingAgent.DefaultSystemPrompt,
                    IsActive = editingAgent.IsActive,
                    LlmConfigId = editingAgent.LlmConfigId
                });
                await Notify.Success("创建成功");
            }

            CloseAgentModal();
            await LoadAgentConfigsAsync();
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
            return;

        try
        {
            await AgentConfigAppService.DeleteAsync(config.Id);
            await Notify.Success("删除成功");
            await LoadAgentConfigsAsync();
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
            await LoadAgentConfigsAsync();
        }
        catch (Exception ex)
        {
            await Notify.Error($"激活失败: {ex.Message}");
        }
    }

    #endregion

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
            await LoadAgentConfigsAsync();
        }
    }

    protected async Task NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            await LoadAgentConfigsAsync();
        }
    }

    protected async Task GoToPage(int page)
    {
        if (page >= 1 && page <= totalPages && page != currentPage)
        {
            currentPage = page;
            await LoadAgentConfigsAsync();
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
