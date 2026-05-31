using Microsoft.Extensions.DependencyInjection;
using AlphaAgent.Maui.Services;

namespace AlphaAgent.Maui;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();
        InitializeTheme();
    }

    private void InitializeTheme()
    {
        var themeManager = new ThemeManager();

        switch (themeManager.CurrentTheme)
        {
            case ThemeMode.Light:
                UserAppTheme = AppTheme.Light;
                break;
            case ThemeMode.Dark:
                UserAppTheme = AppTheme.Dark;
                break;
            case ThemeMode.System:
                UserAppTheme = AppTheme.Unspecified;
                break;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Routing.RegisterRoute("SplashPage", typeof(Views.SplashPage));
        Routing.RegisterRoute("LoginPage", typeof(Views.LoginPage));
        Routing.RegisterRoute("RegisterPage", typeof(Views.RegisterPage));
        // ChatPage, ContactsPage, DiscoveryPage, MePage 已在 AppShell.xaml 中定义
        // 仅注册子页面路由
        Routing.RegisterRoute("ContactDetailPage", typeof(Views.ContactDetailPage));
        Routing.RegisterRoute("ContactMomentsPage", typeof(Views.ContactMomentsPage));
        Routing.RegisterRoute("MomentsPage", typeof(Views.MomentsPage));
        Routing.RegisterRoute("VideoChannelsPage", typeof(Views.VideoChannelsPage));
        Routing.RegisterRoute("AddFriendPage", typeof(Views.AddFriendPage));
        Routing.RegisterRoute("NewFriendsPage", typeof(Views.NewFriendsPage));
        Routing.RegisterRoute("ChatDetailPage", typeof(Views.ChatDetailPage));
        Routing.RegisterRoute("AgentChatDetailPage", typeof(Views.AgentChatDetailPage));
        Routing.RegisterRoute("AgentContactDetailPage", typeof(Views.AgentContactDetailPage));
        Routing.RegisterRoute("InitializingPage", typeof(Views.InitializingPage));
        Routing.RegisterRoute("DeviceManagementPage", typeof(Views.DeviceManagementPage));
        Routing.RegisterRoute("AiSettingsPage", typeof(Views.AiSettingsPage));
        Routing.RegisterRoute("ServiceAccountDetailPage", typeof(Views.ServiceAccountDetailPage));
        Routing.RegisterRoute("ServiceAccountBrowsePage", typeof(Views.ServiceAccountBrowsePage));

        return new Window(new AppShell());
    }
}