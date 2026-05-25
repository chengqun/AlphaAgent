using Microsoft.Extensions.Configuration;
using Volo.Abp.Account;
using Volo.Abp.Mapperly;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Microsoft.Extensions.DependencyInjection;
using AlphaAgent.Abp.Application.Contracts.Services.Security;

namespace AlphaAgent.Abp;

[DependsOn(
    typeof(AbpDomainModule),
    typeof(AbpAccountApplicationModule),
    typeof(AbpApplicationContractsModule),
    typeof(AbpIdentityApplicationModule),
    typeof(AbpPermissionManagementApplicationModule),
    typeof(AbpTenantManagementApplicationModule),
    typeof(AbpFeatureManagementApplicationModule),
    typeof(AbpSettingManagementApplicationModule)
    )]
public class AbpApplicationModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        var connectionString = configuration.GetConnectionString("Default");

        context.Services.AddMapperlyObjectMapper<AbpApplicationModule>();

        // 注册应用服务
        context.Services.AddTransient<AlphaAgent.Abp.Application.Contracts.Services.Relationships.IRelationshipService,
            AlphaAgent.Abp.Application.Services.Relationships.RelationshipService>();
        context.Services.AddTransient<AlphaAgent.Abp.Application.Contracts.Services.Security.ISecurityAppService,
            AlphaAgent.Abp.Application.Services.Security.SecurityAppService>();
        context.Services.AddTransient<AlphaAgent.Abp.Application.Contracts.Services.Devices.IDeviceAppService,
            AlphaAgent.Abp.Application.Services.Devices.DeviceAppService>();
        context.Services.AddTransient<AlphaAgent.Abp.Application.Contracts.Services.Chat.IChatAppService,
            AlphaAgent.Abp.Application.Services.Chat.ChatAppService>();
        context.Services.AddTransient<AlphaAgent.Abp.Application.Contracts.Services.VersionConfig.IVersionConfigAppService,
            AlphaAgent.Abp.Application.Services.VersionConfig.VersionConfigAppService>();

        // Security同步配置
        context.Services.Configure<SecuritySyncOptions>(configuration.GetSection(SecuritySyncOptions.SectionName));
    }
}
