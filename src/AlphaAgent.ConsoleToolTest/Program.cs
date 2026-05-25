using AlphaAgent.Application.Extensions;
using AlphaAgent.Domain.Abstractions;
using AlphaAgent.Domain.Abstractions.AiAgent;
using AlphaAgent.Infrastructure.Extensions;
using AlphaAgent.Infrastructure.Services.AiAgent.Tools;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AlphaAgent.ConsoleToolTest;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 测试工具调用 ===");
        
        // 设置依赖注入
        var services = new ServiceCollection();
        
        // 配置数据库连接
        var dbPath = "test_agent.db";
        var sqliteConnectionString = $"Data Source={dbPath}";
        
        // 删除旧数据库文件
        if (System.IO.File.Exists(dbPath))
        {
            System.IO.File.Delete(dbPath);
            Console.WriteLine($"已删除旧数据库: {dbPath}");
        }
        
        // 配置 Agent 选项
        var agentOptions = new AgentOptions
        {
            ModelName = "deepseek-chat",
            ApiKey = string.Empty,
            Endpoint = "https://api.deepseek.com/v1",
            DefaultSystemPrompt = "你是一个专业的股票分析助手",
            Temperature = 0.5f
        };
        
        // 测试原始错误顺序（Maui中的原始顺序）
        Console.WriteLine("\n测试顺序: AddInfrastructureServices 在前（原始顺序）");
        services.AddInfrastructureServices(sqliteConnectionString, "https://localhost", agentOptions);
        services.AddApplicationServices();
        
        var serviceProvider = services.BuildServiceProvider();
        
        // 初始化数据库
        Console.WriteLine("\n--- 初始化数据库 ---");
        try
        {
            var dbInitializer = serviceProvider.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync();
            Console.WriteLine("✓ 数据库初始化成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ 数据库初始化失败: {ex.Message}");
        }
        
        // 测试 SecurityQueryTool
        Console.WriteLine("\n--- 测试 SecurityQueryTool ---");
        try
        {
            var securityQueryTool = serviceProvider.GetRequiredService<SecurityQueryTool>();
            Console.WriteLine("✓ SecurityQueryTool 实例化成功");
            
            var result = await securityQueryTool.QuerySecurity("浦发");
            Console.WriteLine($"✓ 调用成功: {result.Result.Substring(0, Math.Min(150, result.Result.Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ SecurityQueryTool 调用失败: {ex.Message}");
            Console.WriteLine($"  异常类型: {ex.GetType().Name}");
            if (ex.InnerException != null)
                Console.WriteLine($"  内部异常: {ex.InnerException.Message}");
        }
        
        // 测试 TechnicalAnalysisTool
        Console.WriteLine("\n--- 测试 TechnicalAnalysisTool ---");
        try
        {
            var techAnalysisTool = serviceProvider.GetRequiredService<TechnicalAnalysisTool>();
            Console.WriteLine("✓ TechnicalAnalysisTool 实例化成功");
            
            var result = await techAnalysisTool.CalculateIndicators("600000", "101", "MACD", 10);
            Console.WriteLine($"✓ 调用成功: {result.Result.Substring(0, Math.Min(150, result.Result.Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ TechnicalAnalysisTool 调用失败: {ex.Message}");
            Console.WriteLine($"  异常类型: {ex.GetType().Name}");
            if (ex.InnerException != null)
                Console.WriteLine($"  内部异常: {ex.InnerException.Message}");
        }
        
        // 测试完整的 Agent 调用
        Console.WriteLine("\n--- 测试完整的 Agent 调用 ---");
        try
        {
            var agentFactory = serviceProvider.GetRequiredService<IAgentFactory>();
            var agent = agentFactory.GetAgent("指标分析Agent");
            
            Console.WriteLine("✓ Agent 获取成功");
            Console.WriteLine($"  Agent名称: {agent.Name}");
            Console.WriteLine($"  可用工具: {string.Join(", ", agent.Tools.Select(t => t.Name))}");
            
            var context = new AgentContext
            {
                SessionId = Guid.NewGuid(),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "user", Content = "查询浦发银行的信息" }
                }
            };
            
            var response = await agent.RunAsync(context);
            Console.WriteLine($"\n✓ Agent 响应内容:");
            Console.WriteLine(response.Content);
            
            if (response.ToolCalls != null && response.ToolCalls.Count > 0)
            {
                Console.WriteLine($"\n✓ 工具调用详情:");
                foreach (var tc in response.ToolCalls)
                {
                    Console.WriteLine($"  - 工具名称: {tc.ToolName}");
                    Console.WriteLine($"    - 输入参数: {System.Text.Json.JsonSerializer.Serialize(tc.Input)}");
                    if (tc.Output != null)
                    {
                        Console.WriteLine($"    - 输出结果: {System.Text.Json.JsonSerializer.Serialize(tc.Output)}");
                    }
                }
            }
            else
            {
                Console.WriteLine("\n✗ 没有工具调用");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Agent 调用失败: {ex.Message}");
            Console.WriteLine($"  异常类型: {ex.GetType().Name}");
            if (ex.InnerException != null)
                Console.WriteLine($"  内部异常: {ex.InnerException.Message}");
        }
        
        Console.WriteLine("\n=== 测试完成 ===");
    }
}