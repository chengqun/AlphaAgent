using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Infrastructure.Services.AiAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;

namespace AlphaAgent.Infrastructure.Services.AiAgent.Agents;

public static class StockAnalystNoMemoryAgent
{
    public const string Name = "指标分析Agent(无记忆)";
    public const string Description = "专业的股票分析助手，无记忆模式，每次对话独立，不保留历史上下文";
    public const string DefaultSystemPrompt = "你是一个专业的股票分析助手，可以帮助用户查询股票信息和进行技术指标分析。每次对话独立，不保留历史上下文。请用中文回复。";

    public static IAgent Create(TechnicalAnalysisTool techAnalysisTool, SecurityQueryTool securityQueryTool, IChatClient chatClient, string systemPrompt, float temperature)
    {
        var aiTools = new[]
        {
            AIFunctionFactory.Create(techAnalysisTool.CalculateIndicators),
            AIFunctionFactory.Create(securityQueryTool.QuerySecurity),
        };

        var chatClientAgent = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = Name,
                Description = Description,
            });

        return new LlmAgent(Name, Description, systemPrompt, chatClientAgent, aiTools, temperature,
            memoryMode: AgentMemoryMode.Stateless);
    }
}