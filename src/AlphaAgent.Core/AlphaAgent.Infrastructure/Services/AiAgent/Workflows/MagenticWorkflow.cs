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
/// Magentic-One 工作流：LLM驱动的动态规划与执行。
/// Manager Agent 制定计划、分配任务、检查进度，Worker Agent 执行具体任务。
/// </summary>
public static class MagenticWorkflow
{
    public const string Name = "智能规划工作流";
    public const string Description = "LLM驱动的动态规划工作流，Manager制定计划并分配任务，Worker执行具体任务";
    public const string DefaultSystemPrompt =
        "你是一个智能规划与执行系统。Manager Agent 制定计划并分配任务，Worker Agent 执行具体任务。";

    private static readonly SubAgentDef[] SubAgentDefs =
    [
        new("Manager", "规划与协调", "制定计划、分配任务、检查进度", []),
        new("Worker", "执行者", "执行具体任务",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
    ];

    public static IAgent Create(
        TechnicalAnalysisTool technicalAnalysisTool,
        SecurityQueryTool securityQueryTool,
        IChatClient chatClient,
        string systemPrompt,
        float temperature)
    {
        var toolInstances = new Dictionary<string, AITool>
        {
            [ToolNames.CalculateIndicators] = AIFunctionFactory.Create(technicalAnalysisTool.CalculateIndicators),
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
                "MagenticStockAnalysis",
                subAgents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"构建 Magentic Workflow 失败: {ex.Message}", ex);
        }

        AIAgent workflowAIAgent;
        try
        {
            workflowAIAgent = workflow.AsAIAgent(
                id: "MagenticStockAnalysis",
                name: "MagenticStockAnalysis",
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