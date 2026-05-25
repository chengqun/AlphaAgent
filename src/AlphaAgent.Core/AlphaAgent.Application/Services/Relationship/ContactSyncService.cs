using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Relationship;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Application.Services.Relationship;

public class ContactSyncService : IContactSyncService
{
    private readonly IContactCacheRepository _cacheRepository;
    private readonly IRelationshipService _relationshipService;

    public ContactSyncService(IContactCacheRepository cacheRepository, IRelationshipService relationshipService)
    {
        _cacheRepository = cacheRepository;
        _relationshipService = relationshipService;
    }

    public async Task<ContactBookDto?> GetCachedContactsAsync(Guid userId)
    {
        var items = await _cacheRepository.GetAllAsync(userId);
        if (items.Count == 0) return null;
        return BuildContactBook(items);
    }

    public async Task<ContactBookDto?> SyncFromServerAsync(Guid userId)
    {
        try
        {
            var response = await _relationshipService.GetAcceptedContactsAsync();
            if (!response.Success || response.Data == null)
            {
                return await GetCachedContactsAsync(userId);
            }

            var serverData = response.Data;
            var allDtos = new List<RelationshipDto>();
            allDtos.AddRange(serverData.Friends);
            allDtos.AddRange(serverData.Groups);
            allDtos.AddRange(serverData.Devices);
            allDtos.AddRange(serverData.Stocks);

            var cacheItems = allDtos.Select(dto => MapToCacheItem(dto, userId)).ToList();

            // 全量替换：先清空旧缓存再写入新数据
            await _cacheRepository.DeleteAllAsync(userId);
            await _cacheRepository.UpsertRangeAsync(cacheItems);

            return serverData;
        }
        catch
        {
            return await GetCachedContactsAsync(userId);
        }
    }

    private static ContactBookDto BuildContactBook(List<ContactCacheItem> items)
    {
        var book = new ContactBookDto();

        foreach (var item in items)
        {
            var dto = MapToDto(item);
            switch (item.Type)
            {
                case 0: // Friendship
                    book.Friends.Add(dto);
                    break;
                case 2: // Group
                    book.Groups.Add(dto);
                    break;
                case 1: // Device
                    book.Devices.Add(dto);
                    break;
                case 3: // Stock
                    book.Stocks.Add(dto);
                    break;
            }
        }

        return book;
    }

    private static RelationshipDto MapToDto(ContactCacheItem item)
    {
        return new RelationshipDto
        {
            Id = item.Id,
            Type = item.Type,
            TargetId = item.TargetId,
            TargetName = item.TargetName,
            DeviceType = item.DeviceType,
            Status = item.Status
        };
    }

    private static ContactCacheItem MapToCacheItem(RelationshipDto dto, Guid userId)
    {
        return new ContactCacheItem
        {
            Id = dto.Id,
            Type = dto.Type,
            TargetId = dto.TargetId,
            TargetName = dto.TargetName,
            DeviceType = dto.DeviceType,
            Status = dto.Status,
            CachedAt = DateTime.UtcNow,
            UserId = userId
        };
    }
}
