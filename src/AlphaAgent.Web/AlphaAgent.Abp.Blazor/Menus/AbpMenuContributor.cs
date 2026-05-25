using System.Threading.Tasks;
using AlphaAgent.Abp.Localization;
using AlphaAgent.Abp.MultiTenancy;
using Volo.Abp.Identity.Blazor;
using Volo.Abp.SettingManagement.Blazor.Menus;
using Volo.Abp.TenantManagement.Blazor.Navigation;
using Volo.Abp.UI.Navigation;

namespace AlphaAgent.Abp.Blazor.Menus;

public class AbpMenuContributor : IMenuContributor
{
    public async Task ConfigureMenuAsync(MenuConfigurationContext context)
    {
        if (context.Menu.Name == StandardMenus.Main)
        {
            await ConfigureMainMenuAsync(context);
        }
    }

    private async Task ConfigureMainMenuAsync(MenuConfigurationContext context)
    {
        var administration = context.Menu.GetAdministration();
        var l = context.GetLocalizer<AbpResource>();

        // 添加用户管理菜单
        var userManagement = context.Menu.AddItem(new ApplicationMenuItem(
            "UserManagement",
            l["UserManagement"],
            icon: "fas fa-user-cog"
        ));

        // 朋友圈页面
        var momentsMenu = context.Menu.AddItem(new ApplicationMenuItem(
            AbpMenus.Moments,
            "朋友圈",
            url: "/moments",
            icon: "fas fa-image"
        ));

        // 管理员菜单 - 股票信息管理
        if (await context.IsGrantedAsync("Abp.Securities.Manage"))
        {
            administration.AddItem(new ApplicationMenuItem(
                "SecurityManagement",
                l["SecurityManagement"],
                url: "/security-management",
                icon: "fas fa-shield-alt"
            ));
        }

        // 管理员菜单 - 应用版本管理
        if (await context.IsGrantedAsync("Abp.VersionConfigs.Manage"))
        {
            administration.AddItem(new ApplicationMenuItem(
                "VersionConfigManagement",
                l["VersionConfigManagement"],
                url: "/version-config-management",
                icon: "fas fa-mobile-alt"
            ));
        }

        // 管理员菜单 - Agent配置管理
        if (await context.IsGrantedAsync("Abp.AgentConfigs.Manage"))
        {
            administration.AddItem(new ApplicationMenuItem(
                "AgentConfigManagement",
                l["AgentConfigManagement"],
                url: "/agent-config-management",
                icon: "fas fa-robot"
            ));
        }

        if (MultiTenancyConsts.IsEnabled)
        {
            administration.SetSubItemOrder(TenantManagementMenuNames.GroupName, 1);
        }
        else
        {
            administration.TryRemoveMenuItem(TenantManagementMenuNames.GroupName);
        }

        administration.SetSubItemOrder(IdentityMenuNames.GroupName, 2);
        administration.SetSubItemOrder(SettingManagementMenus.GroupName, 3);
    }
}