using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Application.Dtos.Chat;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Domain.Entities;
using AlphaAgent.Domain.Interfaces;

namespace AlphaAgent.Application.Services.Chat;

public class ConversationSyncService : IConversationSyncService
{
    private readonly IConversationCacheRepository _cacheRepository;
    private readonly IChatService _chatService;

    public ConversationSyncService(IConversationCacheRepository cacheRepository, IChatService chatService)
    {
        _cacheRepository = cacheRepository;
        _chatService = chatService;
    }

    public async Task<List<Conversation>> GetCachedConversationsAsync(Guid userId)
    {
        var items = await _cacheRepository.GetAllAsync(userId);
        return items.Select(MapToConversation).OrderByDescending(c => c.LastMessageTime).ToList();
    }

    public async Task<List<Conversation>> SyncFromServerAsync(Guid userId)
    {
        try
        {
            var response = await _chatService.GetMyConversationsAsync();
            if (!response.Success || response.Data == null)
            {
                // 服务端不可达 — 返回缓存数据
                return await GetCachedConversationsAsync(userId);
            }

            // 过滤掉 Agent 会话（Type 3/4），它们已有独立存储
            var serverConversations = response.Data
                .Where(c => c.Type != 3 && c.Type != 4)
                .ToList();

            var cacheItems = serverConversations.Select(c => MapToCacheItem(c, userId)).ToList();
            await _cacheRepository.UpsertRangeAsync(cacheItems);

            // 删除服务端已不存在的本地缓存
            var serverIds = serverConversations.Select(c => c.Id).ToHashSet();
            var cached = await _cacheRepository.GetAllAsync(userId);
            var deleted = cached.Where(c => !serverIds.Contains(c.Id)).ToList();
            foreach (var item in deleted)
            {
                await _cacheRepository.DeleteAsync(item.Id);
            }

            return await GetCachedConversationsAsync(userId);
        }
        catch
        {
            // 网络错误 — 返回缓存数据
            return await GetCachedConversationsAsync(userId);
        }
    }

    public async Task UpsertConversationAsync(Conversation conversation, Guid userId)
    {
        if (conversation.Type == 3 || conversation.Type == 4) return;
        var item = MapToCacheItem(conversation, userId);
        await _cacheRepository.UpsertAsync(item);
    }

    public async Task DeleteConversationAsync(Guid conversationId)
    {
        await _cacheRepository.DeleteAsync(conversationId);
    }

    private static Conversation MapToConversation(ConversationCacheItem item)
    {
        return new Conversation
        {
            Id = item.Id,
            Type = item.Type,
            Name = item.Name,
            OtherUserName = item.OtherUserName,
            OtherUserId = item.OtherUserId,
            OtherDeviceId = item.OtherDeviceId,
            DeviceType = item.DeviceType,
            UnreadCount = item.UnreadCount,
            LastMessage = item.LastMessage,
            LastMessageTime = item.LastMessageTime,
            MemberCount = item.MemberCount,
            Context = item.Context
        };
    }

    private static ConversationCacheItem MapToCacheItem(Conversation conv, Guid userId)
    {
        return new ConversationCacheItem
        {
            Id = conv.Id,
            Type = conv.Type,
            Name = conv.Name,
            OtherUserName = conv.OtherUserName,
            OtherUserId = conv.OtherUserId,
            OtherDeviceId = conv.OtherDeviceId,
            DeviceType = conv.DeviceType,
            UnreadCount = conv.UnreadCount,
            LastMessage = conv.LastMessage,
            LastMessageTime = conv.LastMessageTime,
            MemberCount = conv.MemberCount,
            Context = conv.Context,
            CachedAt = DateTime.UtcNow,
            UserId = userId
        };
    }
}