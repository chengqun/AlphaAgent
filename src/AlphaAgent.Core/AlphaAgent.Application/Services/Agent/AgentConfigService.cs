using AlphaAgent.Application.Dtos.Agent;
using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Domain.Abstractions.AiAgent;
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
    private readonly IAgentFactory _agentFactory;
    private readonly AgentOptions _agentOptions;

    public AgentConfigService(IHttpClientService httpClientService, IAgentConfigCacheRepository cacheRepository, IAgentFactory agentFactory, AgentOptions agentOptions)
    {
        _httpClientService = httpClientService;
        _cacheRepository = cacheRepository;
        _agentFactory = agentFactory;
        _agentOptions = agentOptions;
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
            modelName = config.ModelName,
            apiKey = config.ApiKey,
            endpoint = config.Endpoint,
            defaultSystemPrompt = config.DefaultSystemPrompt,
            temperature = config.Temperature,
            isActive = config.IsActive
        });
    }

    public async Task EnsureDefaultConfigsAsync(Guid userId, List<AgentConfigResponseDto> existingConfigs)
    {
        var registeredAgents = _agentFactory.GetAvailableAgents();
        var existingNames = existingConfigs.Select(c => c.AgentName).ToHashSet();

        var missingAgents = registeredAgents.Where(a => !existingNames.Contains(a.Name)).ToList();
        if (missingAgents.Count == 0) return;

        // 仅补骨架：AgentName + DefaultSystemPrompt + 模型默认参数，不提交 ApiKey
        foreach (var agent in missingAgents)
        {
            var defaultConfig = new AgentConfigResponseDto
            {
                AgentName = agent.Name,
                ModelName = _agentOptions.ModelName,
                ApiKey = string.Empty,
                Endpoint = _agentOptions.Endpoint,
                DefaultSystemPrompt = agent.SystemPrompt ?? _agentOptions.GetSystemPrompt(agent.Name, string.Empty),
                Temperature = _agentOptions.Temperature,
                IsActive = true
            };

            try
            {
                await SetConfigAsync(defaultConfig);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AgentConfigService] 自动创建配置失败 ({agent.Name}): {ex.Message}");
            }
        }

        // 重新同步以获取服务端分配的 Id
        await SyncFromServerAsync(userId);
    }

    private static AgentConfigCacheItem MapToCacheItem(AgentConfigResponseDto dto, Guid userId)
    {
        return new AgentConfigCacheItem
        {
            Id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid(),
            UserId = userId,
            AgentName = dto.AgentName,
            ModelName = dto.ModelName,
            ApiKey = dto.ApiKey,
            Endpoint = dto.Endpoint,
            DefaultSystemPrompt = dto.DefaultSystemPrompt,
            Temperature = dto.Temperature,
            IsActive = dto.IsActive,
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
            ModelName = item.ModelName,
            ApiKey = item.ApiKey,
            Endpoint = item.Endpoint,
            DefaultSystemPrompt = item.DefaultSystemPrompt,
            Temperature = item.Temperature,
            IsActive = item.IsActive,
            EnabledTools = item.EnabledTools
        };
    }

    private class ListResultDto<T>
    {
        public List<T> Items { get; set; } = new();
    }
}