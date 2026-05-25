using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AlphaAgent.Abp.Domain.Entities;
using AlphaAgent.Abp.Domain.Shared.Enums;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;
using Volo.Abp.Guids;

namespace AlphaAgent.Abp.Domain.Services.Chat;

public class ConversationManager : DomainService, IConversationManager
{
    private readonly IRepository<AppConversation, Guid> _conversationRepository;
    private readonly IRepository<AppConversationParticipant, Guid> _participantRepository;
    private readonly IRepository<AppChatMessage, Guid> _messageRepository;
    private readonly ILogger<ConversationManager> _logger;

    public ConversationManager(
        IRepository<AppConversation, Guid> conversationRepository,
        IRepository<AppConversationParticipant, Guid> participantRepository,
        IRepository<AppChatMessage, Guid> messageRepository,
        ILogger<ConversationManager> logger)
    {
        _conversationRepository = conversationRepository;
        _participantRepository = participantRepository;
        _messageRepository = messageRepository;
        _logger = logger;
    }

    public async Task<AppConversation> GetOrCreateDirectConversationAsync(Guid user1Id, Guid user2Id)
    {
        var key = BuildDirectConversationKey(user1Id, user2Id);

        var existing = await _conversationRepository.FirstOrDefaultAsync(c => c.ConversationKey == key);
        if (existing != null)
        {
            // 检查用户是否仍是参与者（用户可能之前删除过会话）
            var user1ParticipantExists = await _participantRepository.AnyAsync(p =>
                p.ConversationId == existing.Id && p.UserId == user1Id);
            if (!user1ParticipantExists)
            {
                await _participantRepository.InsertAsync(
                    new AppConversationParticipant(GuidGenerator.Create(), existing.Id, user1Id), true);
            }

            var user2ParticipantExists = await _participantRepository.AnyAsync(p =>
                p.ConversationId == existing.Id && p.UserId == user2Id);
            if (!user2ParticipantExists)
            {
                await _participantRepository.InsertAsync(
                    new AppConversationParticipant(GuidGenerator.Create(), existing.Id, user2Id), true);
            }

            return existing;
        }

        var conversation = new AppConversation(
            GuidGenerator.Create(),
            ConversationType.Direct,
            key
        );

        await _conversationRepository.InsertAsync(conversation, true);

        await _participantRepository.InsertAsync(
            new AppConversationParticipant(GuidGenerator.Create(), conversation.Id, user1Id), true);
        await _participantRepository.InsertAsync(
            new AppConversationParticipant(GuidGenerator.Create(), conversation.Id, user2Id), true);

        _logger.LogInformation("Created direct conversation {ConvId} for {User1} and {User2}",
            conversation.Id, user1Id, user2Id);

        return conversation;
    }

    /// <summary>
    /// 创建或获取用户与设备的会话
    /// </summary>
    public async Task<AppConversation> GetOrCreateDeviceConversationAsync(Guid userId, Guid deviceId)
    {
        var key = $"device_{deviceId}";

        var existing = await _conversationRepository.FirstOrDefaultAsync(c => c.ConversationKey == key);
        if (existing != null)
        {
            // 检查用户是否仍是参与者（用户可能之前删除过会话）
            var userParticipantExists = await _participantRepository.AnyAsync(p =>
                p.ConversationId == existing.Id && p.UserId == userId);
            if (!userParticipantExists)
            {
                await _participantRepository.InsertAsync(
                    new AppConversationParticipant(GuidGenerator.Create(), existing.Id, userId), true);
            }
            return existing;
        }

        var conversation = new AppConversation(
            GuidGenerator.Create(),
            ConversationType.Direct,
            key,
            deviceId,
            null
        );

        await _conversationRepository.InsertAsync(conversation, true);

        await _participantRepository.InsertAsync(
            new AppConversationParticipant(GuidGenerator.Create(), conversation.Id, userId), true);
        await _participantRepository.InsertAsync(
            new AppConversationParticipant(GuidGenerator.Create(), conversation.Id, deviceId), true);

        _logger.LogInformation("Created device conversation {ConvId} for user {UserId} and device {DeviceId}",
            conversation.Id, userId, deviceId);

        return conversation;
    }

    public async Task<AppConversation> CreateGroupConversationAsync(Guid groupId, string groupName)
    {
        var key = groupId.ToString();

        var existing = await _conversationRepository.FirstOrDefaultAsync(c => c.ConversationKey == key);
        if (existing != null)
            return existing;

        var conversation = new AppConversation(
            GuidGenerator.Create(),
            ConversationType.Group,
            key,
            groupId,
            groupName
        );

        await _conversationRepository.InsertAsync(conversation, true);

        _logger.LogInformation("Created group conversation {ConvId} for group {GroupId}",
            conversation.Id, groupId);

        return conversation;
    }

    public async Task<AppConversation> GetConversationAsync(Guid conversationId)
    {
        return await _conversationRepository.GetAsync(conversationId);
    }

    public async Task<List<AppConversation>> GetUserConversationsAsync(Guid userId)
    {
        var participantRecords = await _participantRepository.GetListAsync(p => p.UserId == userId);
        var conversationIds = participantRecords.Select(p => p.ConversationId).ToList();
        return await _conversationRepository.GetListAsync(c => conversationIds.Contains(c.Id));
    }

    public async Task<AppChatMessage> SendMessageAsync(Guid conversationId, Guid senderId, string content,
        string messageType = "Text")
    {
        var message = new AppChatMessage(GuidGenerator.Create(), conversationId, senderId, content, messageType);
        await _messageRepository.InsertAsync(message, true);

        // 递增其他参与者的未读数
        var participants = await _participantRepository.GetListAsync(p =>
            p.ConversationId == conversationId && p.UserId != senderId);
        foreach (var participant in participants)
        {
            participant.IncrementUnread();
            await _participantRepository.UpdateAsync(participant);
        }

        return message;
    }

    public async Task MarkAsReadAsync(Guid conversationId, Guid userId)
    {
        var participant = await _participantRepository.FirstOrDefaultAsync(p =>
            p.ConversationId == conversationId && p.UserId == userId);
        if (participant != null)
        {
            participant.MarkAsRead();
            await _participantRepository.UpdateAsync(participant);
        }
    }

    public async Task<int> GetTotalUnreadCountAsync(Guid userId)
    {
        var participants = await _participantRepository.GetListAsync(p => p.UserId == userId);
        return participants.Sum(p => p.UnreadCount);
    }

    public async Task<AppConversationParticipant> AddParticipantAsync(Guid conversationId, Guid userId,
        string role = "Member")
    {
        var existing = await _participantRepository.FirstOrDefaultAsync(p =>
            p.ConversationId == conversationId && p.UserId == userId);
        if (existing != null)
            return existing;

        var participant = new AppConversationParticipant(GuidGenerator.Create(), conversationId, userId, role);
        await _participantRepository.InsertAsync(participant, true);
        return participant;
    }

    public async Task RemoveParticipantAsync(Guid conversationId, Guid userId)
    {
        var participant = await _participantRepository.FirstOrDefaultAsync(p =>
            p.ConversationId == conversationId && p.UserId == userId);
        if (participant != null)
            await _participantRepository.DeleteAsync(participant);
    }

    public async Task EnsureParticipantAsync(Guid conversationId, Guid userId)
    {
        var exists = await _participantRepository.AnyAsync(p =>
            p.ConversationId == conversationId && p.UserId == userId);
        if (!exists)
            throw new BusinessException("AlphaAgent:NotConversationParticipant");
    }

    public async Task<List<AppConversationParticipant>> GetParticipantsAsync(Guid conversationId)
    {
        return await _participantRepository.GetListAsync(p => p.ConversationId == conversationId);
    }

    private static string BuildDirectConversationKey(Guid user1Id, Guid user2Id)
    {
        var ids = new[] { user1Id.ToString(), user2Id.ToString() };
        Array.Sort(ids, StringComparer.Ordinal);
        return $"{ids[0]}|{ids[1]}";
    }
}
