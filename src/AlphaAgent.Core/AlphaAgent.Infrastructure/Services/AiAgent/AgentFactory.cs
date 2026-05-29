using AlphaAgent.Domain.Abstractions.AiAgent;
using System;
using System.Collections.Generic;

namespace AlphaAgent.Infrastructure.Services.AiAgent;

public class AgentFactory : IAgentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, AgentRegistration> _registrations = new();

    public AgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void Register(string name, string description, string defaultSystemPrompt, Func<IServiceProvider, IAgent> factory)
    {
        _registrations[name] = new AgentRegistration(description, defaultSystemPrompt, factory);
    }

    public void Register(string name, string description, string defaultSystemPrompt, List<ToolInfo> allTools, Func<IServiceProvider, IAgent> factory)
    {
        _registrations[name] = new AgentRegistration(description, defaultSystemPrompt, factory, allTools);
    }

    public IAgent GetAgent(string agentName)
    {
        if (_registrations.TryGetValue(agentName, out var registration))
        {
            return registration.Factory(_serviceProvider);
        }

        throw new InvalidOperationException($"未找到智能体: {agentName}");
    }

    public IReadOnlyList<AgentInfo> GetAvailableAgents()
    {
        var infos = new List<AgentInfo>();

        foreach (var (name, registration) in _registrations)
        {
            try
            {
                var agent = registration.Factory(_serviceProvider);
                infos.Add(new AgentInfo
                {
                    Name = agent.Name,
                    Description = agent.Description,
                    SystemPrompt = agent.SystemPrompt,
                    Tools = agent.Tools,
                    MemoryMode = agent.MemoryMode,
                    MaxHistoryMessages = agent.MaxHistoryMessages
                });
            }
            catch
            {
                // ApiKey 为空或 IChatClient 创建失败时，返回注册元数据
                infos.Add(new AgentInfo
                {
                    Name = name,
                    Description = registration.Description,
                    SystemPrompt = registration.DefaultSystemPrompt
                });
            }
        }

        return infos.AsReadOnly();
    }

    public IReadOnlyList<ToolInfo> GetAllTools(string agentName)
    {
        if (_registrations.TryGetValue(agentName, out var registration) && registration.AllTools != null)
            return registration.AllTools;

        // 降级：实例化 Agent 读取（可能已被过滤）
        try
        {
            var agent = registration.Factory(_serviceProvider);
            return agent.Tools;
        }
        catch
        {
            return Array.Empty<ToolInfo>();
        }
    }

    private record AgentRegistration(string Description, string DefaultSystemPrompt, Func<IServiceProvider, IAgent> Factory, List<ToolInfo>? AllTools = null);
}
