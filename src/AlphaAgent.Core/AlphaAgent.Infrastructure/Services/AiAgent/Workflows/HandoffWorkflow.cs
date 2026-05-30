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
/// 分诊路由工作流：分诊 Agent 根据问题类型决定转交给哪个专家。
/// 使用并行工作流+条件路由模拟分诊逻辑。
/// </summary>
public static class HandoffWorkflow
{
    public const string Name = "分诊路由工作流";
    public const string Description = "分诊Agent根据问题类型自动路由到技术分析或投资建议专家";
    public const string DefaultSystemPrompt =
        "你是一个智能分诊助手，根据用户的问题类型决定转交给哪个专家处理。" +
        "技术指标相关的问题交给技术分析专家，投资建议相关的问题交给投资建议专家。" +
        "请用中文回复。";

    private static readonly SubAgentDef[] SubAgentDefs =
    [
        new("TriageAgent", "分诊助手", "分析用户问题类型，决定转交给哪个专家",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
        new("TechExpert", "技术分析专家", "处理技术指标、行情数据相关的问题",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
        new("AdviceExpert", "投资建议专家", "处理投资方向、仓位建议相关的问题", []),
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
                "HandoffStockAnalysis",
                subAgents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"构建 Handoff Workflow 失败: {ex.Message}", ex);
        }

        AIAgent workflowAIAgent;
        try
        {
            workflowAIAgent = workflow.AsAIAgent(
                id: "HandoffStockAnalysis",
                name: "HandoffStockAnalysis",
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