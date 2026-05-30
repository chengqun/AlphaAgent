using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Infrastructure.Services.AiAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AlphaAgent.Infrastructure.Services.AiAgent.Workflows;

/// <summary>
/// 并行分析工作流：技术面、基本面、情绪面同时分析，最后聚合结果。
/// 三个子 Agent 并行处理同一输入，结果合并输出。
/// </summary>
public static class ConcurrentAnalysisWorkflow
{
    public const string Name = "并行分析工作流";
    public const string Description = "并行执行技术面、基本面、情绪面分析并聚合结果的工作流";
    public const string DefaultSystemPrompt =
        "你是一个并行股票分析工作流，将同时从技术面、基本面和情绪面三个维度分析股票。" +
        "每个维度的分析结果将聚合成综合报告。请用中文回复。";

    private static readonly SubAgentDef[] SubAgentDefs =
    [
        new("TechAnalyst", "技术面分析师", "从技术指标、K线形态、趋势线等角度分析股票走势",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
        new("FundAnalyst", "基本面分析师", "从财务数据、行业地位、公司基本面等角度评估股票价值", []),
        new("SentimentAnalyst", "情绪面分析师", "从市场情绪、资金流向、舆论热点等角度判断市场情绪", []),
    ];

    public static IAgent Create(
        TechnicalAnalysisTool techAnalysisTool,
        SecurityQueryTool securityQueryTool,
        IChatClient chatClient,
        string systemPrompt,
        float temperature)
    {
        var toolInstances = new Dictionary<string, AITool>
        {
            [ToolNames.CalculateIndicators] = AIFunctionFactory.Create(techAnalysisTool.CalculateIndicators),
            [ToolNames.QuerySecurity] = AIFunctionFactory.Create(securityQueryTool.QuerySecurity),
        };

        var subAgents = SubAgentDefs.Select(def => 
        {
            var tools = def.ToolNames.Any() 
                ? def.ToolNames.Select(name => toolInstances[name]).ToList() 
                : null;
                
            return new ChatClientAgent(
                chatClient,
                new ChatClientAgentOptions
                {
                    Name = def.Name,
                    Description = $"{def.DisplayName} - {def.Description}",
                    ChatOptions = new ChatOptions
                    {
                        Tools = tools,
                        Temperature = temperature,
                    },
                });
        }).ToArray();

        Workflow workflow;
        try
        {
            workflow = AgentWorkflowBuilder.BuildSequential(
                "ConcurrentStockAnalysis",
                subAgents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"构建 Concurrent Workflow 失败: {ex.Message}", ex);
        }

        AIAgent workflowAIAgent;
        try
        {
            workflowAIAgent = workflow.AsAIAgent(
                id: "ConcurrentStockAnalysis",
                name: "ConcurrentStockAnalysis",
                description: Description,
                executionEnvironment: InProcessExecution.OffThread);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Workflow.AsAIAgent() 失败: {ex.Message}", ex);
        }

        var workflowAgent = new WorkflowAgent(
            Name, Description, systemPrompt, workflowAIAgent, temperature);

        foreach (var def in SubAgentDefs)
        {
            workflowAgent.SubAgents.Add(new WorkflowSubAgentInfo
            {
                Name = def.Name,
                DisplayName = def.DisplayName,
                Description = def.Description,
                Tools = def.ToolDefs.Select(t => new WorkflowToolInfo
                {
                    Name = t.Name,
                    Description = t.Description,
                }).ToList(),
            });
        }

        return workflowAgent;
    }

    private record SubAgentDef(string Name, string DisplayName, string Description, ToolDef[] ToolDefs)
    {
        public string[] ToolNames => ToolDefs.Select(t => t.Name).ToArray();
    }

    private record ToolDef(string Name, string Description);
}