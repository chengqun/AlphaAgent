using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Moment;
using AlphaAgent.Application.Interfaces.Moment;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Application.Services.Moment;

public class MomentCacheService : IMomentCacheService
{
    private readonly IMomentCacheRepository _repository;

    public MomentCacheService(IMomentCacheRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<MomentDto>> GetCachedMomentsAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<List<MomentDto>> GetCachedMomentsAsync(string targetId)
    {
        var items = await _repository.GetAllAsync();
        return items.Where(i => i.TargetId == targetId).Select(ToDto).ToList();
    }

    public async Task<DateTime?> GetLatestCachedCreatedAtAsync()
    {
        return await _repository.GetLatestCreatedAtAsync();
    }

    public async Task UpdateCacheAsync(List<MomentDto> moments)
    {
        var items = moments.Select(ToItem);
        await _repository.AddOrUpdateAsync(items);
    }

    public async Task AddMomentAsync(MomentDto moment)
    {
        await _repository.AddAsync(ToItem(moment));
    }

    public async Task ClearCacheAsync()
    {
        await _repository.ClearAllAsync();
    }

    private static MomentDto ToDto(MomentCacheItem item) => new()
    {
        Id = item.Id,
        UserId = item.UserId,
        Username = item.Username,
        Content = item.Content,
        ImageUrl = item.ImageUrl,
        CreatedAt = item.CreatedAt,
        Type = item.Type,
        Visibility = item.Visibility,
        TargetId = item.TargetId
    };

    private static MomentCacheItem ToItem(MomentDto dto) => new()
    {
        Id = dto.Id,
        UserId = dto.UserId,
        Username = dto.Username,
        Content = dto.Content,
        ImageUrl = dto.ImageUrl,
        CreatedAt = dto.CreatedAt,
        Type = dto.Type,
        Visibility = dto.Visibility,
        TargetId = dto.TargetId
    };
}
