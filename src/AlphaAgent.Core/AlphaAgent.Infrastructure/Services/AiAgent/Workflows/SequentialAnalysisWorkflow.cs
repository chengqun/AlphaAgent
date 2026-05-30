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
/// 顺序分析工作流：技术分析 → 风险评估 → 投资建议
/// 三个子 Agent 依次处理，前一阶段的输出作为下一阶段的输入。
/// </summary>
public static class SequentialAnalysisWorkflow
{
    public const string Name = "顺序分析工作流";
    public const string Description = "顺序执行技术分析、风险评估和投资建议的股票分析工作流";
    public const string DefaultSystemPrompt =
        "你是一个专业的股票分析工作流，将依次执行技术指标分析、风险评估和投资建议生成。" +
        "每个阶段的输出将作为下一阶段的输入，最终给出综合分析报告。请用中文回复。";

    /// <summary>
    /// 子 Agent 定义：集中管理名称、描述、工具。增删工具只需改此处。
    /// </summary>
    private static readonly SubAgentDef[] SubAgentDefs =
    [
        new("TechnicalAnalyst", "技术分析专家", "执行技术指标分析，查询行情数据并计算SMA/EMA/RSI/MACD等指标",
        [
            new ToolDef(ToolNames.CalculateIndicators, "计算股票的技术指标"),
            //new ToolDef(ToolNames.QuerySecurity, "查询证券信息"),
        ]),
        new("RiskAssessor", "风险评估专家", "基于技术分析结果评估风险水平，给出风险等级和提示", []),
        new("InvestmentAdvisor", "投资建议专家", "综合分析和风险评估，给出操作方向、仓位和止损止盈建议", []),
    ];

    public static IAgent Create(
        TechnicalAnalysisTool techAnalysisTool,
        SecurityQueryTool securityQueryTool,
        IChatClient chatClient,
        string systemPrompt,
        float temperature)
    {
        // 工具名 → AITool 实例的映射（子 Agent 共享同一个工具池，按定义选取）
        var toolInstances = new Dictionary<string, AITool>
        {
            [ToolNames.CalculateIndicators] = AIFunctionFactory.Create(techAnalysisTool.CalculateIndicators),
            [ToolNames.QuerySecurity] = AIFunctionFactory.Create(securityQueryTool.QuerySecurity),
        };

        // 按定义表创建子 Agent
        var subAgents = SubAgentDefs.Select(def => new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = def.Name,
                Description = $"{def.DisplayName} - {def.Description}",
                ChatOptions = new ChatOptions
                {
                    Tools = def.ToolNames.Select(name => toolInstances[name]).ToList(),
                    Temperature = temperature,
                },
            })).ToArray();

        // 构建 Sequential Workflow
        Workflow workflow;
        try
        {
            workflow = AgentWorkflowBuilder.BuildSequential(
                "SequentialStockAnalysisWorkflow", subAgents);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"构建 Sequential Workflow 失败: {ex.Message}", ex);
        }

        // Workflow 转为 AIAgent
        AIAgent workflowAIAgent;
        try
        {
            workflowAIAgent = workflow.AsAIAgent(
                id: "SequentialStockAnalysis",
                name: "SequentialStockAnalysis",
                description: Description,
                executionEnvironment: InProcessExecution.OffThread);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Workflow.AsAIAgent() 失败: {ex.Message}", ex);
        }

        // 包装为 WorkflowAgent 适配器，从定义表填充 UI 信息
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

    /// <summary>
    /// 子 Agent 定义（名称、显示名、描述、工具列表）。
    /// 增删子 Agent 或工具只需修改 <see cref="SubAgentDefs"/> 数组。
    /// </summary>
    private record SubAgentDef(
        string Name,
        string DisplayName,
        string Description,
        ToolDef[] ToolDefs)
    {
        /// <summary>工具名列表，供创建 ChatClientAgent 时从 toolInstances 选取</summary>
        public string[] ToolNames => ToolDefs.Select(t => t.Name).ToArray();
    }

    /// <summary>
    /// 工具定义（名称 + 描述）。与 <see cref="ToolNames"/> 常量对应。
    /// </summary>
    private record ToolDef(string Name, string Description);
}