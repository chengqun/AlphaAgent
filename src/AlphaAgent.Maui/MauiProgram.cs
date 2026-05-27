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
			ModelName = "deepseek-chat",
			ApiKey = string.Empty,
			Endpoint = "https://api.deepseek.com/v1",
			Temperature = 0.5f
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

		// 注册 ViewModel
		builder.Services.AddScoped<LoginViewModel>();
		builder.Services.AddScoped<RegisterViewModel>();
		builder.Services.AddScoped<SplashViewModel>();
		builder.Services.AddScoped<ChatViewModel>();
		builder.Services.AddScoped<ContactsViewModel>();
		builder.Services.AddScoped<ContactDetailViewModel>();
		builder.Services.AddScoped<DiscoveryViewModel>();
		builder.Services.AddScoped<MeViewModel>();
		builder.Services.AddScoped<MomentsViewModel>();
		builder.Services.AddScoped<ContactMomentsViewModel>();
		builder.Services.AddScoped<VideoChannelsViewModel>();
		builder.Services.AddScoped<AddFriendViewModel>();
		builder.Services.AddScoped<NewFriendsViewModel>();
		builder.Services.AddScoped<ChatDetailViewModel>();
		builder.Services.AddScoped<AgentChatDetailViewModel>();
		builder.Services.AddScoped<AgentContactDetailViewModel>();
        builder.Services.AddScoped<InitializingViewModel>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		return app;
	}
}
