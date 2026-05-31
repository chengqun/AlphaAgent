using AlphaAgent.Application.Interfaces.Agent;
using AlphaAgent.Application.Interfaces.Security;
using AlphaAgent.Application.Interfaces.Auth;
using AlphaAgent.Application.Interfaces.Chat;
using AlphaAgent.Application.Interfaces.Relationship;
using AlphaAgent.Application.Interfaces.Moment;
using AlphaAgent.Application.Interfaces.Common;
using AlphaAgent.Application.Interfaces.Video;
using AlphaAgent.Application.Interfaces.Update;
using AlphaAgent.Application.Interfaces.Device;
using AlphaAgent.Application.Interfaces.ServiceAccount;
using AlphaAgent.Application.Services.Agent;
using AlphaAgent.Application.Services.Security;
using AlphaAgent.Application.Services.Auth;
using AlphaAgent.Application.Services.Chat;
using AlphaAgent.Application.Services.Relationship;
using AlphaAgent.Application.Services.Moment;
using AlphaAgent.Application.Services.Common;
using AlphaAgent.Application.Services.Video;
using AlphaAgent.Application.Services.Update;
using AlphaAgent.Application.Services.Device;
using AlphaAgent.Application.Services.ServiceAccount;
using AlphaAgent.Domain.Services.Security;
using Microsoft.Extensions.DependencyInjection;

namespace AlphaAgent.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 无状态服务：Singleton 语义明确，避免 AddScoped 在 MAUI 中的歧义
        services.AddSingleton<ISecurityManager, SecurityManager>();

        services.AddSingleton<ISecurityService, SecurityService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IRelationshipService, RelationshipService>();
        services.AddSingleton<IGroupService, GroupService>();
        services.AddSingleton<IMomentService, MomentService>();
        services.AddSingleton<IMomentCacheService, MomentCacheService>();
        services.AddSingleton<IChatService, ChatService>();
        services.AddSingleton<IConversationSyncService, ConversationSyncService>();
        services.AddSingleton<IContactSyncService, ContactSyncService>();
        services.AddSingleton<IAgentService, AgentService>();
        services.AddSingleton<IMessageCacheService, MessageCacheService>();
        services.AddSingleton<IVideoFeedService, VideoFeedService>();
        services.AddSingleton<IUpdateService, UpdateService>();
        services.AddSingleton<ISecurityClientSyncService, SecurityClientSyncService>();
        services.AddSingleton<IAgentConfigService, AgentConfigService>();
        services.AddSingleton<ILlmConfigService, LlmConfigService>();
        services.AddSingleton<IDeviceService, DeviceService>();
        services.AddSingleton<IServiceAccountService, ServiceAccountService>();

        // 一次性初始化：每次调用获取新实例
        services.AddTransient<ICoreInitializer, CoreInitializer>();
        services.AddTransient<IPostLoginInitializer, PostLoginInitializer>();

        return services;
    }
}
