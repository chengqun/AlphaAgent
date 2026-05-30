using AlphaAgent.Abp.Localization;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;

namespace AlphaAgent.Abp.Permissions;

public class AbpPermissionDefinitionProvider : PermissionDefinitionProvider
{
    public override void Define(IPermissionDefinitionContext context)
    {
        var myGroup = context.AddGroup(AbpPermissions.GroupName);

        // 设备管理权限
        var devicePermission = myGroup.AddPermission(AbpPermissions.Devices.Default, L("Permission:Devices"));
        devicePermission.AddChild(AbpPermissions.Devices.Manage, L("Permission:Devices.Manage"));
        devicePermission.AddChild(AbpPermissions.Devices.View, L("Permission:Devices.View"));

        // 消息管理权限
        var messagePermission = myGroup.AddPermission(AbpPermissions.Messages.Default, L("Permission:Messages"));
        messagePermission.AddChild(AbpPermissions.Messages.Manage, L("Permission:Messages.Manage"));
        messagePermission.AddChild(AbpPermissions.Messages.Send, L("Permission:Messages.Send"));
        messagePermission.AddChild(AbpPermissions.Messages.Receive, L("Permission:Messages.Receive"));

        // 好友管理权限
        var friendshipPermission = myGroup.AddPermission(AbpPermissions.Friendships.Default, L("Permission:Friendships"));
        friendshipPermission.AddChild(AbpPermissions.Friendships.Manage, L("Permission:Friendships.Manage"));
        friendshipPermission.AddChild(AbpPermissions.Friendships.View, L("Permission:Friendships.View"));

        // 群组管理权限
        var groupPermission = myGroup.AddPermission(AbpPermissions.Groups.Default, L("Permission:Groups"));
        groupPermission.AddChild(AbpPermissions.Groups.Manage, L("Permission:Groups.Manage"));
        groupPermission.AddChild(AbpPermissions.Groups.View, L("Permission:Groups.View"));

        // 朋友圈权限
        var momentPermission = myGroup.AddPermission(AbpPermissions.Moments.Default, L("Permission:Moments"));
        momentPermission.AddChild(AbpPermissions.Moments.Manage, L("Permission:Moments.Manage"));
        momentPermission.AddChild(AbpPermissions.Moments.View, L("Permission:Moments.View"));

        // 股票管理权限
        var stockPermission = myGroup.AddPermission(AbpPermissions.Stocks.Default, L("Permission:Stocks"));
        stockPermission.AddChild(AbpPermissions.Stocks.Manage, L("Permission:Stocks.Manage"));
        stockPermission.AddChild(AbpPermissions.Stocks.View, L("Permission:Stocks.View"));

        // 证券管理权限（股票基本信息）
        var securityPermission = myGroup.AddPermission(AbpPermissions.Securities.Default, L("Permission:Securities"));
        securityPermission.AddChild(AbpPermissions.Securities.Manage, L("Permission:Securities.Manage"));
        securityPermission.AddChild(AbpPermissions.Securities.Create, L("Permission:Securities.Create"));
        securityPermission.AddChild(AbpPermissions.Securities.Update, L("Permission:Securities.Update"));
        securityPermission.AddChild(AbpPermissions.Securities.Delete, L("Permission:Securities.Delete"));

        // OpenAI配置权限
        var openAiPermission = myGroup.AddPermission(AbpPermissions.OpenAI.Default, L("Permission:OpenAI"));
        openAiPermission.AddChild(AbpPermissions.OpenAI.Manage, L("Permission:OpenAI.Manage"));

        // Agent Prompt权限
        var agentPromptPermission = myGroup.AddPermission(AbpPermissions.AgentPrompts.Default, L("Permission:AgentPrompts"));
        agentPromptPermission.AddChild(AbpPermissions.AgentPrompts.Manage, L("Permission:AgentPrompts.Manage"));

        // OpenIddict客户端管理权限
        var openIddictPermission = myGroup.AddPermission(AbpPermissions.OpenIddict.Default, L("Permission:OpenIddict"));
        openIddictPermission.AddChild(AbpPermissions.OpenIddict.ClientManagement, L("Permission:ClientManagement"));

        // 关系管理权限
        var relationshipPermission = myGroup.AddPermission(AbpPermissions.Relationships.Default, L("Permission:Relationships"));
        relationshipPermission.AddChild(AbpPermissions.Relationships.Manage, L("Permission:Relationships.Manage"));
        relationshipPermission.AddChild(AbpPermissions.Relationships.View, L("Permission:Relationships.View"));
        relationshipPermission.AddChild(AbpPermissions.Relationships.Create, L("Permission:Relationships.Create"));
        relationshipPermission.AddChild(AbpPermissions.Relationships.Delete, L("Permission:Relationships.Delete"));

        // 聊天权限
        var chatPermission = myGroup.AddPermission(AbpPermissions.Chat.Default, L("Permission:Chat"));
        chatPermission.AddChild(AbpPermissions.Chat.Send, L("Permission:Chat.Send"));
        chatPermission.AddChild(AbpPermissions.Chat.View, L("Permission:Chat.View"));

        // Agent配置管理权限
        var agentConfigPermission = myGroup.AddPermission(AbpPermissions.AgentConfigs.Default, L("Permission:AgentConfigs"));
        agentConfigPermission.AddChild(AbpPermissions.AgentConfigs.Manage, L("Permission:AgentConfigs.Manage"));
        agentConfigPermission.AddChild(AbpPermissions.AgentConfigs.Create, L("Permission:AgentConfigs.Create"));
        agentConfigPermission.AddChild(AbpPermissions.AgentConfigs.Update, L("Permission:AgentConfigs.Update"));
        agentConfigPermission.AddChild(AbpPermissions.AgentConfigs.Delete, L("Permission:AgentConfigs.Delete"));

        // LLM配置管理权限
        var llmConfigPermission = myGroup.AddPermission(AbpPermissions.LlmConfigs.Default, L("Permission:LlmConfigs"));
        llmConfigPermission.AddChild(AbpPermissions.LlmConfigs.Manage, L("Permission:LlmConfigs.Manage"));
        llmConfigPermission.AddChild(AbpPermissions.LlmConfigs.Create, L("Permission:LlmConfigs.Create"));
        llmConfigPermission.AddChild(AbpPermissions.LlmConfigs.Update, L("Permission:LlmConfigs.Update"));
        llmConfigPermission.AddChild(AbpPermissions.LlmConfigs.Delete, L("Permission:LlmConfigs.Delete"));

        // 应用版本管理权限
        var versionConfigPermission = myGroup.AddPermission(AbpPermissions.VersionConfigs.Default, L("Permission:VersionConfigs"));
        versionConfigPermission.AddChild(AbpPermissions.VersionConfigs.Manage, L("Permission:VersionConfigs.Manage"));
        versionConfigPermission.AddChild(AbpPermissions.VersionConfigs.Create, L("Permission:VersionConfigs.Create"));
        versionConfigPermission.AddChild(AbpPermissions.VersionConfigs.Update, L("Permission:VersionConfigs.Update"));
        versionConfigPermission.AddChild(AbpPermissions.VersionConfigs.Delete, L("Permission:VersionConfigs.Delete"));
    }

    private static LocalizableString L(string name)
    {
        return LocalizableString.Create<AbpResource>(name);
    }
}
