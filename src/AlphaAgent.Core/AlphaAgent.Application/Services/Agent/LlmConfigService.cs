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

public class LlmConfigService : ILlmConfigService
{
    private readonly IHttpClientService _httpClientService;
    private readonly ILlmConfigCacheRepository _cacheRepository;

    public LlmConfigService(IHttpClientService httpClientService, ILlmConfigCacheRepository cacheRepository)
    {
        _httpClientService = httpClientService;
        _cacheRepository = cacheRepository;
    }

    public async Task<List<LlmConfigResponseDto>?> GetCachedConfigsAsync(Guid userId)
    {
        var items = await _cacheRepository.GetByUserIdAsync(userId);
        if (items.Count == 0) return null;
        return items.Select(MapToDto).ToList();
    }

    public async Task<List<LlmConfigResponseDto>?> SyncFromServerAsync(Guid userId)
    {
        try
        {
            var result = await _httpClientService.GetAsync<ListResultDto<LlmConfigResponseDto>>("api/app/llm-config/my-llm-configs");
            if (result?.Items == null || result.Items.Count == 0)
            {
                return await GetCachedConfigsAsync(userId);
            }

            var serverItems = result.Items;
            var cacheItems = serverItems.Select(dto => MapToCacheItem(dto, userId)).ToList();

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

    public async Task SetConfigAsync(LlmConfigResponseDto config)
    {
        await _httpClientService.PostAsync<LlmConfigResponseDto>("api/app/llm-config/set-my-llm-config", new
        {
            id = config.Id,
            name = config.Name,
            modelName = config.ModelName,
            apiKey = config.ApiKey,
            endpoint = config.Endpoint,
            temperature = config.Temperature,
            isDefault = config.IsDefault
        });
    }

    private static LlmConfigCacheItem MapToCacheItem(LlmConfigResponseDto dto, Guid userId)
    {
        return new LlmConfigCacheItem
        {
            Id = dto.Id != Guid.Empty ? dto.Id : Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name,
            ModelName = dto.ModelName,
            ApiKey = dto.ApiKey,
            Endpoint = dto.Endpoint,
            Temperature = dto.Temperature,
            IsDefault = dto.IsDefault,
            CachedAt = DateTime.UtcNow
        };
    }

    private static LlmConfigResponseDto MapToDto(LlmConfigCacheItem item)
    {
        return new LlmConfigResponseDto
        {
            Id = item.Id,
            Name = item.Name,
            ModelName = item.ModelName,
            ApiKey = item.ApiKey,
            Endpoint = item.Endpoint,
            Temperature = item.Temperature,
            IsDefault = item.IsDefault
        };
    }

    private class ListResultDto<T>
    {
        public List<T> Items { get; set; } = new();
    }
}