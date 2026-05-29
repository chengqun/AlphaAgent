using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Infrastructure.Services.AiAgent.Tools;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
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

    private const string TechnicalAnalystPrompt =
        "你是技术分析专家。根据用户提供的股票信息，使用工具查询行情数据并计算技术指标。" +
        "输出详细的技术分析结果，包括趋势判断、关键价位、指标信号等。请用中文回复。";

    private const string RiskAssessorPrompt =
        "你是风险评估专家。基于技术分析结果，评估该股票的风险水平。" +
        "考虑波动性、趋势稳定性、超买超卖状态等因素，给出风险等级和风险提示。请用中文回复。";

    private const string InvestmentAdvisorPrompt =
        "你是投资建议专家。综合技术分析结果和风险评估，给出具体的投资建议。" +
        "包括操作方向（买入/卖出/持有）、仓位建议、止损止盈价位等。请用中文回复。";

    public static IAgent Create(
        TechnicalAnalysisTool techAnalysisTool,
        SecurityQueryTool securityQueryTool,
        IChatClient chatClient,
        string systemPrompt,
        float temperature)
    {
        // 构建技术分析工具（仅第一个子 Agent 需要工具）
        var analysisTools = new AITool[]
        {
            AIFunctionFactory.Create(techAnalysisTool.CalculateIndicators),
            AIFunctionFactory.Create(securityQueryTool.QuerySecurity),
        };

        // 子 Agent 1：技术分析专家（带工具）
        var technicalAnalyst = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "技术分析专家",
                Description = "执行技术指标分析",
                ChatOptions = new ChatOptions
                {
                    Tools = analysisTools.ToList(),
                    Temperature = temperature,
                },
            });

        // 子 Agent 2：风险评估专家（纯文本推理，无工具）
        var riskAssessor = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "风险评估专家",
                Description = "评估股票风险水平",
                ChatOptions = new ChatOptions
                {
                    Temperature = temperature,
                },
            });

        // 子 Agent 3：投资建议专家（纯文本推理，无工具）
        var investmentAdvisor = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "投资建议专家",
                Description = "生成投资建议",
                ChatOptions = new ChatOptions
                {
                    Temperature = temperature,
                },
            });

        // 构建 Sequential Workflow
        var workflow = AgentWorkflowBuilder.BuildSequential(
            "SequentialStockAnalysis",
            new[] { technicalAnalyst, riskAssessor, investmentAdvisor });

        // Workflow 转为 AIAgent
        var workflowAIAgent = workflow.AsAIAgent(
            id: "sequential-stock-analysis",
            name: Name,
            description: Description,
            executionEnvironment: InProcessExecution.Default);

        // 包装为 WorkflowAgent 适配器
        return new WorkflowAgent(
            Name, Description, systemPrompt, workflowAIAgent, temperature);
    }
}