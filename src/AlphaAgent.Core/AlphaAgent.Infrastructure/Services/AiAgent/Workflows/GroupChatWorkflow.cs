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
/// 圆桌讨论工作流：多个专家围绕同一问题轮流发言讨论，达成共识后输出。
/// 使用顺序工作流模拟轮番发言，每位专家依次处理上一位专家的输出。
/// </summary>
public static class GroupChatWorkflow
{
    public const string Name = "圆桌讨论工作流";
    public const string Description = "多专家轮流讨论股票问题，达成共识后输出综合意见";
    public const string DefaultSystemPrompt =
        "你是一个圆桌讨论工作流，多位专家将围绕用户的股票问题轮流发表观点。" +
        "每位专家从自己的专业角度分析，最终形成综合意见。请用中文回复。";

    private static readonly SubAgentDef[] SubAgentDefs =
    [
        new("BullAnalyst", "看多分析师", "从乐观角度分析股票上涨的可能性和利好因素", []),
        new("BearAnalyst", "看空分析师", "从谨慎角度分析股票下跌的风险和利空因素", []),
        new("Moderator", "主持人", "综合多空观点，给出客观中立的投资建议",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
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
                "GroupChatStockAnalysis",
                subAgents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"构建 GroupChat Workflow 失败: {ex.Message}", ex);
        }

        AIAgent workflowAIAgent;
        try
        {
            workflowAIAgent = workflow.AsAIAgent(
                id: "GroupChatStockAnalysis",
                name: "GroupChatStockAnalysis",
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