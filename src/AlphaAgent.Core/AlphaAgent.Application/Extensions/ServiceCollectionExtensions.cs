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
using AlphaAgent.Domain.Services.Security;
using AlphaAgent.Domain.Services.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace AlphaAgent.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ISecurityManager, SecurityManager>();
        services.AddScoped<ITokenManager, TokenManager>();

        services.AddScoped<ISecurityService, SecurityService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRelationshipService, RelationshipService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IMomentService, MomentService>();
        services.AddScoped<IMomentCacheService, MomentCacheService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IConversationSyncService, ConversationSyncService>();
        services.AddScoped<IContactSyncService, ContactSyncService>();
        services.AddScoped<ICoreInitializer, CoreInitializer>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IMessageCacheService, MessageCacheService>();
        services.AddScoped<IVideoFeedService, VideoFeedService>();
        services.AddScoped<IUpdateService, UpdateService>();
        services.AddScoped<ISecurityClientSyncService, SecurityClientSyncService>();
        services.AddScoped<IAgentConfigService, AgentConfigService>();
        services.AddScoped<IPostLoginInitializer, PostLoginInitializer>();
        services.AddScoped<IDeviceService, DeviceService>();

        return services;
    }
}
