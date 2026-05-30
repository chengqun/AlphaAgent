using Microsoft.Extensions.Logging;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Application.Extensions;
using AlphaAgent.Infrastructure.Extensions;
using AlphaAgent.Maui.ViewModels;
using AlphaAgent.Maui.Services;
using Syncfusion.Maui.Toolkit.Hosting;
using CommunityToolkit.Maui;

namespace AlphaAgent.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		// 初始化自定义证书处理器（从内嵌 rootCA.crt 加载自签名 CA 证书）
		CustomCertificateHandler.Initialize();

		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkitMediaElement(isAndroidForegroundServiceEnabled: true)
			.ConfigureSyncfusionToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// 注册 Core 服务，使用 SQLite 数据库
		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "alphaagent.db");
		var sqliteConnectionString = $"Data Source={dbPath}";

		// 配置 LLM Agent
		var agentOptions = new AgentOptions
		{
			DefaultLlm = new LlmOptions
			{
				Name = "DeepSeek",
				ModelName = "deepseek-chat",
				ApiKey = string.Empty,
				Endpoint = "https://api.deepseek.com/v1",
				Temperature = 0.5f
			}
		};

		// 使用 Core 项目的服务注册扩展方法，传入自定义 HttpMessageHandler 工厂
		builder.Services.AddInfrastructureServices(sqliteConnectionString, AppSettings.ServerBaseAddress, agentOptions, CustomCertificateHandler.CreateHandler);
		builder.Services.AddApplicationServices();

		// 注册主题服务
		builder.Services.AddSingleton<IThemeManager, ThemeManager>();

		// 注册事件总线服务
		builder.Services.AddSingleton<IEventBusService, EventBusService>();

		// 注册未读消息缓存服务
		builder.Services.AddSingleton<IUnreadMessageCacheService, UnreadMessageCacheService>();

		// 注册全局消息处理器
		builder.Services.AddSingleton<IGlobalMessageHandler, GlobalMessageHandler>();

		// TabBar 主页：Shell 只创建一个实例，用 Singleton 保持状态
		builder.Services.AddSingleton<SplashViewModel>();
		builder.Services.AddSingleton<ChatViewModel>();
		builder.Services.AddSingleton<ContactsViewModel>();
		builder.Services.AddSingleton<DiscoveryViewModel>();
		builder.Services.AddSingleton<MeViewModel>();

		// 子页面：每次导航创建新实例，避免状态串扰
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<ContactDetailViewModel>();
		builder.Services.AddTransient<MomentsViewModel>();
		builder.Services.AddTransient<ContactMomentsViewModel>();
		builder.Services.AddTransient<VideoChannelsViewModel>();
		builder.Services.AddTransient<AddFriendViewModel>();
		builder.Services.AddTransient<NewFriendsViewModel>();
		builder.Services.AddTransient<ChatDetailViewModel>();
		builder.Services.AddTransient<AgentChatDetailViewModel>();
		builder.Services.AddTransient<AgentContactDetailViewModel>();
		builder.Services.AddTransient<InitializingViewModel>();
		builder.Services.AddTransient<DeviceManagementViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		return app;
	}
}
