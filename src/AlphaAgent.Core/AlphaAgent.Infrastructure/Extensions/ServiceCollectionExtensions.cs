using AlphaAgent.Domain.Abstractions;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Domain.Abstractions.Interfaces;
using AlphaAgent.Domain.Interfaces;
using AlphaAgent.Domain.Services.Auth;
using AlphaAgent.Domain.Services.Security;
using AlphaAgent.Infrastructure.Interfaces;
using AlphaAgent.Infrastructure.Data;
using AlphaAgent.Infrastructure.Data.Repositories;
using AlphaAgent.Infrastructure.Services.AiAgent;
using AlphaAgent.Infrastructure.Services.AiAgent.Agents;
using AlphaAgent.Infrastructure.Services.AiAgent.Tools;
using AlphaAgent.Infrastructure.Services.AiAgent.Workflows;
using AlphaAgent.Infrastructure.Services.Database;
using AlphaAgent.Infrastructure.Services.Http;
using AlphaAgent.Infrastructure.Services.Indicators;
using AlphaAgent.Infrastructure.Services.Quotes;
using AlphaAgent.Infrastructure.Services.Quotes.Providers;
using AlphaAgent.Infrastructure.Services.Chat;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Net.Http;

namespace AlphaAgent.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string sqliteConnectionString,
        string baseAddress,
        AgentOptions agentOptions,
        Func<HttpMessageHandler>? httpMessageHandlerFactory = null)
    {
        services.AddDbContextFactory<SharesDbContext>(options =>
            options.UseSqlite(sqliteConnectionString));

        // Repository：通过 IDbContextFactory 每次创建 DbContext，Repository 本身无状态
        services.AddSingleton<ISecurityRepository, SecurityRepository>();
        services.AddSingleton<IQuoteRepository, QuoteRepository>();
        services.AddSingleton<ITokenRepository, TokenRepository>();
        services.AddSingleton<IAgentRepository, AgentRepository>();
        services.AddSingleton<IMessageCacheRepository, MessageCacheRepository>();
        services.AddSingleton<IConversationCacheRepository, ConversationCacheRepository>();
        services.AddSingleton<IContactCacheRepository, ContactCacheRepository>();
        services.AddSingleton<IMomentCacheRepository, MomentCacheRepository>();
        services.AddSingleton<IVideoFeedRepository, VideoFeedRepository>();
        services.AddSingleton<IAgentConfigCacheRepository, AgentConfigCacheRepository>();

        // 一次性使用：每次获取新实例
        services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();
        services.AddTransient<ISyncMetadataStore, SyncMetadataStore>();

        // TokenManager: 使用独立 HttpClient（不带 BearerTokenDelegatingHandler，避免循环依赖）
        services.AddSingleton<ITokenManager>(sp =>
        {
            var tokenRepository = sp.GetRequiredService<ITokenRepository>();
            HttpMessageHandler handler = httpMessageHandlerFactory != null
                ? httpMessageHandlerFactory()
                : new SocketsHttpHandler();
            var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseAddress) };
            return new TokenManager(tokenRepository, httpClient);
        });

        // DelegatingHandler 必须是 Transient：AddHttpClient 每次构建管道会设置 InnerHandler，不能复用同一实例
        services.AddTransient<BearerTokenDelegatingHandler>();

        var httpClientBuilder = services.AddHttpClient<IHttpClientService, HttpClientService>(client =>
        {
            client.BaseAddress = new Uri(baseAddress);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        httpClientBuilder.AddHttpMessageHandler(sp => sp.GetRequiredService<BearerTokenDelegatingHandler>());

        if (httpMessageHandlerFactory != null)
        {
            httpClientBuilder.ConfigurePrimaryHttpMessageHandler(() => httpMessageHandlerFactory());
        }

        services.AddHttpClient<SinaQuoteProvider>();
        services.AddHttpClient<BaiduQuoteProvider>();
        services.AddHttpClient<EastQuoteProvider>();

        services.AddSingleton<IQuoteProvider, SinaQuoteProvider>();
        services.AddSingleton<IQuoteProvider, BaiduQuoteProvider>();
        services.AddSingleton<IQuoteProvider, EastQuoteProvider>();
        services.AddSingleton<IFailoverQuoteProvider, FailoverQuoteProvider>();

        services.AddSingleton<IIndicatorCalculator, IndicatorCalculator>();

        services.AddSingleton<IAnalysisManager, AnalysisManager>();

        RegisterAgentServices(services, agentOptions);

        services.AddSingleton<ISignalRChatService>(sp =>
        {
            var tokenManager = sp.GetRequiredService<ITokenManager>();
            return new SignalRChatService(tokenManager, httpMessageHandlerFactory);
        });

        return services;
    }

    private static void RegisterAgentServices(IServiceCollection services, AgentOptions options)
    {
        services.AddSingleton(options);

        // 注册所有 Tools (Transient 避免 Scope 泄漏)
        services.AddTransient<TechnicalAnalysisTool>();
        services.AddTransient<SecurityQueryTool>();

        // 注册 Agent 工厂：每次创建 Agent 时动态构建 IChatClient
        // 这样登录后更新 AgentOptions，下次创建 Agent 会使用最新配置
        services.AddSingleton<IAgentFactory>(sp =>
        {
            var factory = new AgentFactory(sp);

            var allTools = new List<ToolInfo>
            {
                new() { Name = ToolNames.CalculateIndicators, Description = "计算股票的技术指标，包括SMA、EMA、RSI、MACD、BB、SAR、KDJ、ADX等" },
                new() { Name = ToolNames.QuerySecurity, Description = "查询证券信息，根据关键词搜索股票/期货代码和名称" },
            };

            factory.Register(StockAnalystAgent.Name, StockAnalystAgent.Description, StockAnalystAgent.DefaultSystemPrompt, allTools, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(StockAnalystAgent.Name, StockAnalystAgent.DefaultSystemPrompt);
                var enabledTools = opts.GetEnabledTools(StockAnalystAgent.Name)?.ToArray();
                return StockAnalystAgent.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature, enabledTools);
            });

            factory.Register(StockAnalystNoMemoryAgent.Name, StockAnalystNoMemoryAgent.Description, StockAnalystNoMemoryAgent.DefaultSystemPrompt, allTools, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(StockAnalystNoMemoryAgent.Name, StockAnalystNoMemoryAgent.DefaultSystemPrompt);
                var enabledTools = opts.GetEnabledTools(StockAnalystNoMemoryAgent.Name)?.ToArray();
                return StockAnalystNoMemoryAgent.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature, enabledTools);
            });

            factory.Register(SequentialAnalysisWorkflow.Name, SequentialAnalysisWorkflow.Description, SequentialAnalysisWorkflow.DefaultSystemPrompt, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(SequentialAnalysisWorkflow.Name, SequentialAnalysisWorkflow.DefaultSystemPrompt);
                return SequentialAnalysisWorkflow.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature);
            });

            factory.Register(ConcurrentAnalysisWorkflow.Name, ConcurrentAnalysisWorkflow.Description, ConcurrentAnalysisWorkflow.DefaultSystemPrompt, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(ConcurrentAnalysisWorkflow.Name, ConcurrentAnalysisWorkflow.DefaultSystemPrompt);
                return ConcurrentAnalysisWorkflow.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature);
            });

            factory.Register(GroupChatWorkflow.Name, GroupChatWorkflow.Description, GroupChatWorkflow.DefaultSystemPrompt, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(GroupChatWorkflow.Name, GroupChatWorkflow.DefaultSystemPrompt);
                return GroupChatWorkflow.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature);
            });

            factory.Register(HandoffWorkflow.Name, HandoffWorkflow.Description, HandoffWorkflow.DefaultSystemPrompt, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(HandoffWorkflow.Name, HandoffWorkflow.DefaultSystemPrompt);
                return HandoffWorkflow.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature);
            });

            factory.Register(MagenticWorkflow.Name, MagenticWorkflow.Description, MagenticWorkflow.DefaultSystemPrompt, serviceProvider =>
            {
                var opts = serviceProvider.GetRequiredService<AgentOptions>();
                var chatClient = CreateChatClient(opts);
                var systemPrompt = opts.GetSystemPrompt(MagenticWorkflow.Name, MagenticWorkflow.DefaultSystemPrompt);
                return MagenticWorkflow.Create(
                    serviceProvider.GetRequiredService<TechnicalAnalysisTool>(),
                    serviceProvider.GetRequiredService<SecurityQueryTool>(),
                    chatClient, systemPrompt, opts.Temperature);
            });

            return factory;
        });
    }

    private static IChatClient CreateChatClient(AgentOptions opts)
    {
        var openAiClient = new OpenAI.OpenAIClient(
            new ApiKeyCredential(opts.ApiKey),
            new OpenAI.OpenAIClientOptions { Endpoint = new Uri(opts.Endpoint) }
        );
        return openAiClient.GetChatClient(opts.ModelName).AsIChatClient();
    }
}