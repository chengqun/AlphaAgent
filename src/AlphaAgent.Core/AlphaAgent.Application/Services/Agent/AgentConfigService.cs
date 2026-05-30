using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Agent;

public class AgentConfigService : IAgentConfigService
{
    private readonly IHttpClientService _httpClientService;
    private readonly IAgentConfigCacheRepository _cacheRepository;

    public AgentConfigService(IHttpClientService httpClientService, IAgentConfigCacheRepository cacheRepository)
    {
        _httpClientService = httpClientService;
        _cacheRepository = cacheRepository;
    }

    public async Task<List<AgentConfigResponseDto>?> GetCachedConfigsAsync(Guid userId)
    {
        var items = await _cacheRepository.GetByUserIdAsync(userId);
        if (items.Count == 0) return null;
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<AgentConfigResponseDto>?> SyncFromServerAsync(Guid userId)
    {
        try
        {
            var result = await _httpClientService.GetAsync<ListResultDto<AgentConfigResponseDto>>("api/app/agent-config/my-config");
            if (result?.Items == null || result.Items.Count == 0)
            {
                return await GetCachedConfigsAsync(userId);
            }

            // 全量替换前：保存本地 EnabledTools（服务端不存此字段，避免被清空）
            var existingItems = await _cacheRepository.GetByUserIdAsync(userId);
            var localEnabledTools = existingItems
                .Where(c => c.EnabledTools.Count > 0)
                .ToDictionary(c => c.AgentName, c => c.EnabledTools);

            var serverItems = result.Items;
            var cacheItems = serverItems.Select(dto =>
            {
                var item = MapToCacheItem(dto, userId);
                // 回填本地保存的 EnabledTools
                if (localEnabledTools.TryGetValue(dto.AgentName, out var tools))
                    item.EnabledTools = tools;
                return item;
            }).ToList();

            // 全量替换：先清空旧缓存再写入新数据
            await _cacheRepository.DeleteByUserIdAsync(userId);
            await _cacheRepository.UpsertRangeAsync(cacheItems);

            return serverItems;
        }
        catch (Exception)
        {
            return await GetCachedConfigsAsync(userId);
        }
    }

    public async Task SetConfigAsync(AgentConfigResponseDto config)
    {
        await _httpClientService.PostAsync<AgentConfigResponseDto>("api/app/agent-config/set-my-config", new
        {
            agentName = config.AgentName,
            defaultSystemPrompt = config.DefaultSystemPrompt,
            isActive = config.IsActive,
            llmConfigId = config.LlmConfigId
        });
    }

    public async Task EnsureDefaultConfigsAsync(Guid userId, List<AgentConfigResponseDto> existingConfigs)
    {
        // 不再为缺失的 Agent 创建空 ApiKey 占位配置。
        // LLM 配置已独立到 AppLlmConfig，Agent 配置仅含 AgentName + DefaultSystemPrompt + LlmConfigId。
        // 服务端 AgentConfigAppService.SetMyConfigAsync 可按需创建。
    }

    private static AgentConfigCacheItem MapToCacheItem(AgentConfigResponseDto dto, Guid userId)
    {
        return new AgentConfigCacheItem
        {
            Id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid(),
            UserId = userId,
            AgentName = dto.AgentName,
            DefaultSystemPrompt = dto.DefaultSystemPrompt,
            IsActive = dto.IsActive,
            LlmConfigId = dto.LlmConfigId,
            EnabledTools = dto.EnabledTools,
            CachedAt = DateTime.UtcNow
        };
    }

    private static AgentConfigResponseDto MapToDto(AgentConfigCacheItem item)
    {
        return new AgentConfigResponseDto
        {
            Id = item.Id,
            AgentName = item.AgentName,
            DefaultSystemPrompt = item.DefaultSystemPrompt,
            IsActive = item.IsActive,
            LlmConfigId = item.LlmConfigId,
            EnabledTools = item.EnabledTools
        };
    }

    private class ListResultDto<T>
    {
        public List<T> Items { get; set; } = new();
    }
}