using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Domain.Abstractions.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AlphaAgent.Application.Services.Chat;

public class MessageCacheService : IMessageCacheService
{
    private readonly IMessageCacheRepository _messageCacheRepository;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MessageCacheService(IMessageCacheRepository messageCacheRepository)
    {
        _messageCacheRepository = messageCacheRepository;
    }

    public async Task<List<ChatMessage>> GetCachedMessagesAsync(Guid conversationId, int? limit = null)
    {
        var json = await _messageCacheRepository.GetCachedMessagesJsonAsync(conversationId);
        if (string.IsNullOrEmpty(json))
            return new List<ChatMessage>();

        try
        {
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json, _jsonOptions) ?? new List<ChatMessage>();
            
            // 如果设置了限制，只返回最新的N条消息
            if (limit.HasValue && limit.Value > 0 && messages.Count > limit.Value)
            {
                messages = messages.Skip(messages.Count - limit.Value).ToList();
            }
            
            return messages;
        }
        catch
        {
            return new List<ChatMessage>();
        }
    }

    public async Task<DateTime?> GetLastCachedAtAsync(Guid conversationId)
    {
        return await _messageCacheRepository.GetLastCachedAtAsync(conversationId);
    }

    public async Task CacheMessagesAsync(Guid conversationId, List<ChatMessage> messages, int? maxCacheSize = null)
    {
        // 如果设置了最大缓存大小，只保留最新的N条消息
        if (maxCacheSize.HasValue && maxCacheSize.Value > 0 && messages.Count > maxCacheSize.Value)
        {
            messages = messages.Skip(messages.Count - maxCacheSize.Value).ToList();
        }
        
        var json = JsonSerializer.Serialize(messages, _jsonOptions);
        await _messageCacheRepository.CacheMessagesJsonAsync(conversationId, json);
    }

    public async Task AppendMessageAsync(Guid conversationId, ChatMessage message)
    {
        var json = JsonSerializer.Serialize(message, _jsonOptions);
        await _messageCacheRepository.AppendMessageJsonAsync(conversationId, json);
    }

    public async Task ClearCacheAsync(Guid conversationId)
    {
        await _messageCacheRepository.ClearCacheAsync(conversationId);
    }
}